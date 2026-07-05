using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SharedCommon.Messaging.DeadLetterQueue;

/// <summary>
/// Handles poison messages and dead-letter queue scenarios with retry logic.
/// Automatically routes messages that fail processing to a DLQ for later inspection and recovery.
/// </summary>
/// <remarks>
/// Process flow:
/// 1. Attempt message processing with configurable retries
/// 2. Smart exception filtering (retryable vs non-retryable)
/// 3. Exponential backoff between retries
/// 4. Route to DLQ after max retries or non-retryable exception
/// 5. Store full context for manual/automated recovery
/// </remarks>
public interface IPoisonMessageHandler
{
    /// <summary>
    /// Processes a message with automatic retry and DLQ routing.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="messageId">Unique message identifier for tracking</param>
    /// <param name="processor">The message processing function</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Processing result indicating success, DLQ routing, or error</returns>
    /// <exception cref="ArgumentNullException">Thrown when message, messageId, or processor is null</exception>
    Task<MessageProcessingResult> ProcessWithRetryAsync<T>(
        T message,
        string messageId,
        Func<T, CancellationToken, Task> processor,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Routes a failed message to the dead-letter queue for later inspection and recovery.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message that failed</param>
    /// <param name="messageId">Unique message identifier for tracking</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Processing result indicating DLQ routing status</returns>
    /// <exception cref="ArgumentNullException">Thrown when message, messageId, or exception is null</exception>
    Task<MessageProcessingResult> RouteToDeadLetterQueueAsync<T>(
        T message,
        string messageId,
        Exception exception,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Processes a dead-letter message with a custom recovery handler.
    /// Used by background jobs or manual intervention to retry or inspect failed messages.
    /// </summary>
    /// <param name="dlm">The dead-letter message to process</param>
    /// <param name="handler">The recovery processing function</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Processing result indicating recovery success or continued failure</returns>
    /// <exception cref="ArgumentNullException">Thrown when dlm or handler is null</exception>
    Task<MessageProcessingResult> ProcessDeadLetterMessageAsync(
        DeadLetterMessage dlm,
        Func<string, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of message processing indicating success, failure, or DLQ routing.
/// </summary>
public class MessageProcessingResult
{
    /// <summary>
    /// Gets or sets whether the message was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of processing attempts used.
    /// </summary>
    public int AttemptsUsed { get; set; }

    /// <summary>
    /// Gets or sets whether the message was routed to the dead-letter queue.
    /// </summary>
    public bool RoutedToDLQ { get; set; }

    /// <summary>
    /// Gets or sets an error message if processing failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Configuration options for poison message handling.
/// </summary>
public class PoisonMessageOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts before routing to DLQ.
    /// Default: 3 retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay in milliseconds for exponential backoff.
    /// Default: 100ms.
    /// Example: 1st retry after 100ms, 2nd after 200ms, 3rd after 400ms.
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the DLQ message retention period.
    /// Messages older than this are eligible for purging.
    /// Default: 30 days.
    /// </summary>
    public TimeSpan DLQRetention { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets whether to enable automatic DLQ message recovery (background job).
    /// Default: true.
    /// </summary>
    public bool EnableAutomaticRetry { get; set; } = true;
}

/// <summary>
/// Represents a message that failed processing and is stored in the dead-letter queue.
/// Contains full context for later inspection and recovery.
/// </summary>
public class DeadLetterMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for this DLQ message.
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the original message type (fully qualified name).
    /// </summary>
    public string OriginalMessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized original message JSON.
    /// Contains full message context for recovery attempts.
    /// </summary>
    public string OriginalMessageJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets information about the exception that caused the failure.
    /// </summary>
    public ExceptionInfo? Exception { get; set; }

    /// <summary>
    /// Gets or sets when the message failed processing.
    /// </summary>
    public DateTime FailureTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of processing attempts made before DLQ routing.
    /// </summary>
    public int ProcessingAttempts { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the failure or recovery status.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Exception information captured for DLQ message diagnosis and recovery.
/// </summary>
public class ExceptionInfo
{
    /// <summary>
    /// Gets or sets the exception type name (fully qualified).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception stack trace for debugging.
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Default implementation of <see cref="IPoisonMessageHandler"/>.
/// Provides automatic retry with exponential backoff and DLQ routing for failed messages.
/// </summary>
public sealed class PoisonMessageHandler : IPoisonMessageHandler
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PoisonMessageHandler> _logger;
    private readonly PoisonMessageOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoisonMessageHandler"/> class.
    /// </summary>
    /// <param name="messagePublisher">Publisher for routing messages to DLQ</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <param name="options">Configuration for retry and DLQ behavior</param>
    /// <exception cref="ArgumentNullException">Thrown when messagePublisher, logger, or options is null</exception>
    public PoisonMessageHandler(
        IMessagePublisher messagePublisher,
        ILogger<PoisonMessageHandler> logger,
        PoisonMessageOptions options)
    {
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<MessageProcessingResult> ProcessWithRetryAsync<T>(
        T message,
        string messageId,
        Func<T, CancellationToken, Task> processor,
        CancellationToken cancellationToken = default) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException(nameof(messageId));
        if (processor == null) throw new ArgumentNullException(nameof(processor));

        var attemptCount = 0;
        Exception? lastException = null;

        while (attemptCount < _options.MaxRetries)
        {
            try
            {
                attemptCount++;
                _logger.LogDebug(
                    "Processing message (attempt {AttemptCount}/{MaxRetries}): {MessageId}, Type: {MessageType}",
                    attemptCount, _options.MaxRetries, messageId, typeof(T).Name);

                await processor(message, cancellationToken);

                _logger.LogInformation(
                    "Message processed successfully after {AttemptCount} attempt(s): {MessageId}",
                    attemptCount, messageId);

                return new MessageProcessingResult { Success = true, AttemptsUsed = attemptCount };
            }
            catch (Exception ex) when (ShouldRetry(ex) && attemptCount < _options.MaxRetries)
            {
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "Retryable error processing message (attempt {AttemptCount}/{MaxRetries}): {MessageId}",
                    attemptCount, _options.MaxRetries, messageId);

                // Exponential backoff: 100ms, 200ms, 400ms, ...
                var delay = TimeSpan.FromMilliseconds(
                    _options.InitialDelayMs * Math.Pow(2, attemptCount - 1));

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(
                    ex,
                    "Non-retryable error or max retries exceeded processing message: {MessageId}",
                    messageId);
                break;
            }
        }

        // All retries exhausted, route to DLQ
        return await RouteToDeadLetterQueueAsync(message, messageId, lastException!, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MessageProcessingResult> RouteToDeadLetterQueueAsync<T>(
        T message,
        string messageId,
        Exception exception,
        CancellationToken cancellationToken = default) where T : class
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException(nameof(messageId));
        if (exception == null) throw new ArgumentNullException(nameof(exception));

        try
        {
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
            await _messagePublisher.PublishAsync(deadLetterMessage, cancellationToken);

            _logger.LogError(
                "Message routed to dead-letter queue: {MessageId}, MessageType: {MessageType}, Exception: {ExceptionType}",
                messageId, typeof(T).Name, exception.GetType().Name);

            return new MessageProcessingResult
            {
                Success = false,
                AttemptsUsed = _options.MaxRetries,
                RoutedToDLQ = true,
                Error = $"Routed to DLQ after {_options.MaxRetries} attempts: {exception.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Critical: Failed to route message to dead-letter queue: {MessageId}",
                messageId);

            return new MessageProcessingResult
            {
                Success = false,
                AttemptsUsed = _options.MaxRetries,
                Error = $"Failed to route to DLQ: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<MessageProcessingResult> ProcessDeadLetterMessageAsync(
        DeadLetterMessage dlm,
        Func<string, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        if (dlm == null) throw new ArgumentNullException(nameof(dlm));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        try
        {
            _logger.LogInformation(
                "Processing dead-letter message for recovery: {MessageId}, Type: {OriginalMessageType}",
                dlm.MessageId, dlm.OriginalMessageType);

            await handler(dlm.OriginalMessageJson, cancellationToken);

            _logger.LogInformation("Dead-letter message recovered successfully: {MessageId}", dlm.MessageId);

            return new MessageProcessingResult
            {
                Success = true,
                AttemptsUsed = dlm.ProcessingAttempts + 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to recover dead-letter message: {MessageId}",
                dlm.MessageId);

            return new MessageProcessingResult
            {
                Success = false,
                AttemptsUsed = dlm.ProcessingAttempts + 1,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Determines if an exception is retryable.
    /// Retryable: transient failures (timeout, connection errors)
    /// Non-retryable: permanent failures (validation, deserialization, authorization)
    /// </summary>
    /// <param name="ex">The exception to evaluate</param>
    /// <returns>True if the exception should trigger a retry</returns>
    private static bool ShouldRetry(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            InvalidOperationException when ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) => true,
            IOException when ex.InnerException is TimeoutException => true,
            _ => false
        };
    }
}
