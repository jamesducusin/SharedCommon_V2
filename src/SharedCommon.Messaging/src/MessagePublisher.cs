using MassTransit;
using SharedCommon.Core;

namespace SharedCommon.Messaging;

/// <summary>
/// Default <see cref="IMessagePublisher"/> that publishes via MassTransit and
/// propagates the current request's correlation ID in message headers.
/// </summary>
public sealed class MessagePublisher(
    IPublishEndpoint publishEndpoint,
    IRequestContext requestContext) : IMessagePublisher
{
    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class =>
        publishEndpoint.Publish(message, ctx => SetCorrelationId(ctx), cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync<T>(
        T message,
        Action<PublishContext<T>> configure,
        CancellationToken cancellationToken = default)
        where T : class =>
        publishEndpoint.Publish(message, ctx =>
        {
            SetCorrelationId(ctx);
            configure(ctx);
        }, cancellationToken);

    private void SetCorrelationId(PublishContext context)
    {
        var correlationValue = requestContext.CorrelationId.Value;

        // MassTransit CorrelationId is Guid-based; use the header as the canonical propagation path
        context.Headers.Set("x-correlation-id", correlationValue);

        if (Guid.TryParse(correlationValue, out var guid))
            context.CorrelationId = guid;
    }
}
