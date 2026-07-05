using MassTransit;

namespace SharedCommon.Messaging;

/// <summary>
/// Publishes domain events to the message broker.
/// Automatically propagates the current request's correlation ID.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>Publishes <paramref name="message"/> to all subscribed consumers.</summary>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes <paramref name="message"/> with additional <see cref="PublishContext{T}"/> configuration.</summary>
    Task PublishAsync<T>(
        T message,
        Action<PublishContext<T>> configure,
        CancellationToken cancellationToken = default)
        where T : class;
}
