using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedCommon.Observability;

namespace SharedCommon.Messaging.DeadLetterQueue;

/// <summary>
/// Handles poison messages and dead-letter queue scenarios.
/// Automatically routes messages that fail processing to a DLQ for later inspection.
/// </summary>
public class PoisonMessageHandler : IPoisonMessageHandler
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PoisonMessageHandler> _logger;
    private readonly ITelemetryService _telemetry;
    private readonly PoisonMessageOptions _options;

    public PoisonMessageHandler(
        IMessagePublisher messagePublisher,
        ILogger<PoisonMessageHandler> logger,
        ITelemetryService telemetry,
        PoisonMessageOptions options)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes a message with automatic retry and DLQ routing.
    /// </summary>
    public async Task<MessageProcessingResult> ProcessWithRetryAsync<T>(
        T message,
        string messageId,
        Func<T, CancellationToken, Task> processor,
        CancellationToken cancellationToken = default) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (processor == null) throw new ArgumentNullException(nameof(processor));

        using var scope = _telemetry.StartOperation("ProcessMessageWithRetry", "messaging");
        scope.SetTag("message.id", messageId);
        scope.SetTag("message.type", typeof(T).Name);

        var attemptCount = 0;
        Exception? lastException = null;

        while (attemptCount < _options.MaxRetries)
        {
            try
            {
                attemptCount++;
                scope.SetTag("retry.attempt", attemptCount.ToString());

                await processor(message, cancellationToken);

                _telemetry.RecordMetric("message.processed", 1, new()
                {
                    { "message_type", typeof(T).Name },
                    { "attempts", attemptCount.ToString() }
                });

                _logger.LogInformation(
                    "Message processed successfully after {AttemptCount} attempt(s): {MessageId}",
                    attemptCount, messageId);

                scope.MarkSucceeded();
                return new MessageProcessingResult { Success = true, AttemptsUsed = attemptCount };
            }
            catch (Exception ex) when (ShouldRetry(ex) && attemptCount < _options.MaxRetries)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "Message processing failed (attempt {AttemptCount}/{MaxRetries}): {MessageId}",
                    attemptCount, _options.MaxRetries, messageId);

                // Exponential backoff before retry
                var delay = TimeSpan.FromMilliseconds(
                    _options.InitialDelayMs * Math.Pow(2, attemptCount - 1));

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                // Non-retryable exception or max retries exceeded
                _logger.LogError(
                    ex,
                    "Message processing failed (non-retryable or max retries exceeded): {MessageId}",
                    messageId);
                break;
            }
        }

        // All retries exhausted, route to DLQ
        return await RouteToDeadLetterQueueAsync(message, messageId, lastException!, cancellationToken);
    }

    /// <summary>
    /// Routes a failed message to the dead-letter queue for later inspection and recovery.
    /// </summary>
    public async Task<MessageProcessingResult> RouteToDeadLetterQueueAsync<T>(
        T message,
        string messageId,
        Exception exception,
        CancellationToken cancellationToken = default) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (exception == null) throw new ArgumentNullException(nameof(exception));

        try
        {
            using var scope = _telemetry.StartOperation("RouteToDLQ", "messaging");
            scope.SetTag("message.id", messageId);
            scope.SetTag("exception.type", exception.GetType().Name);
            scope.RecordException(exception);

            var deadLetterMessage = new DeadLetterMessage
            {
                MessageId = messageId,
                OriginalMessageType = typeof(T).FullName ?? typeof(T).Name,
                OriginalMessageJson = System.Text.Json.JsonSerializer.Serialize(message),
                Exception = new ExceptionInfo
                {
                    Type = exception.GetType().FullName ?? exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                },
                FailureTime = DateTime.UtcNow,
                ProcessingAttempts = _options.MaxRetries
            };

            // Publish to DLQ
            await _messagePublisher.PublishAsync(
                deadLetterMessage,
                $"dlq.{typeof(T).Name}",
                cancellationToken);

            _telemetry.RecordMetric("message.dlq", 1, new()
            {
                { "message_type", typeof(T).Name },
                { "exception_type", exception.GetType().Name }
            });

            _logger.LogError(
                "Message routed to dead-letter queue: {MessageId} (Exception: {ExceptionType})",
                messageId, exception.GetType().Name);

            scope.MarkSucceeded();
            return new MessageProcessingResult
            {
                Success = false,
                AttemptsUsed = _options.MaxRetries,
                RoutedToDLQ = true,
                Error = $"Routed to DLQ: {exception.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Failed to route message to dead-letter queue: {MessageId}",
                messageId);

            return new MessageProcessingResult
            {
                Success = false,
                AttemptsUsed = _options.MaxRetries,
                Error = $"Failed to route to DLQ: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Determines if an exception should trigger a retry.
    /// Retryable: timeouts, transient errors, connection issues
    /// Non-retryable: validation errors, deserialization errors, authorization failures
    /// </summary>
    private bool ShouldRetry(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            InvalidOperationException when ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) => true,
            IOException when ex.InnerException is TimeoutException => true,
            _ => false
        };
    }

    /// <summary>
    /// Processes a dead-letter message with a custom handler.
    /// Used by background jobs to retry or inspect failed messages.
    /// </summary>
    public async Task<MessageProcessingResult> ProcessDeadLetterMessageAsync(
        DeadLetterMessage dlm,
        Func<string, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        if (dlm == null) throw new ArgumentNullException(nameof(dlm));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        using var scope = _telemetry.StartOperation("ProcessDLQMessage", "messaging");
        scope.SetTag("message.id", dlm.MessageId);
        scope.SetTag("original.message_type", dlm.OriginalMessageType);

        try
        {
            await handler(dlm.OriginalMessageJson, cancellationToken);

            _logger.LogInformation("Dead-letter message recovered: {MessageId}", dlm.MessageId);
            scope.MarkSucceeded();

            return new MessageProcessingResult
            {
                Success = true,
                AttemptsUsed = dlm.ProcessingAttempts + 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover dead-letter message: {MessageId}", dlm.MessageId);
            scope.RecordException(ex);

            return new MessageProcessingResult
            {
                Success = false,
                AttemptsUsed = dlm.ProcessingAttempts + 1,
                Error = ex.Message
            };
        }
    }
}

public interface IPoisonMessageHandler
{
    Task<MessageProcessingResult> ProcessWithRetryAsync<T>(
        T message,
        string messageId,
        Func<T, CancellationToken, Task> processor,
        CancellationToken cancellationToken = default) where T : class;

    Task<MessageProcessingResult> RouteToDeadLetterQueueAsync<T>(
        T message,
        string messageId,
        Exception exception,
        CancellationToken cancellationToken = default) where T : class;

    Task<MessageProcessingResult> ProcessDeadLetterMessageAsync(
        DeadLetterMessage dlm,
        Func<string, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}

public class PoisonMessageOptions
{
    /// <summary>
    /// Maximum number of retry attempts before routing to DLQ. Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay in milliseconds for exponential backoff. Default: 100ms
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// DLQ expiration time. Messages older than this are purged. Default: 30 days
    /// </summary>
    public TimeSpan DLQRetention { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Enable automatic DLQ processing (background job). Default: true
    /// </summary>
    public bool EnableAutomaticRetry { get; set; } = true;
}

/// <summary>
/// Represents a message that failed processing and is in the dead-letter queue.
/// </summary>
public class DeadLetterMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string OriginalMessageType { get; set; } = string.Empty;
    public string OriginalMessageJson { get; set; } = string.Empty;
    public ExceptionInfo? Exception { get; set; }
    public DateTime FailureTime { get; set; } = DateTime.UtcNow;
    public int ProcessingAttempts { get; set; }
    public string? Notes { get; set; }
}

public class ExceptionInfo
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}

public class MessageProcessingResult
{
    public bool Success { get; set; }
    public int AttemptsUsed { get; set; }
    public bool RoutedToDLQ { get; set; }
    public string? Error { get; set; }
}
