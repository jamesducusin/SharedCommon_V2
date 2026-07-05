using MassTransit;
using Microsoft.Extensions.Logging;

namespace SharedCommon.Messaging.Filters;

/// <summary>
/// MassTransit consume filter that emits structured log entries at the start and end of each message.
/// </summary>
public sealed class LoggingFilter<T>(ILogger<LoggingFilter<T>> logger) : IFilter<ConsumeContext<T>>
    where T : class
{
    private const string CorrelationIdHeader = "x-correlation-id";

    /// <inheritdoc />
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageType = typeof(T).Name;
        var messageId = context.MessageId;
        var correlationId = context.Headers.Get<string>(CorrelationIdHeader)
            ?? context.CorrelationId?.ToString();

        logger.LogInformation(
            "Consuming {MessageType} {MessageId} {CorrelationId}",
            messageType, messageId, correlationId);

        try
        {
            await next.Send(context);

            logger.LogInformation(
                "Consumed {MessageType} {MessageId} {CorrelationId}",
                messageType, messageId, correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed consuming {MessageType} {MessageId} {CorrelationId}",
                messageType, messageId, correlationId);
            throw;
        }
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context) => context.CreateFilterScope("logging");
}
