namespace SharedCommon.Cloud;

/// <summary>
/// Abstraction over cloud queue services (Azure Service Bus, AWS SQS).
/// Provides simple send/receive primitives for cloud-native queuing.
/// </summary>
public interface ICloudQueueService
{
    /// <summary>
    /// Sends a message to the specified queue.
    /// </summary>
    /// <typeparam name="T">Message payload type — must be JSON-serializable.</typeparam>
    /// <param name="queueName">Queue or topic name.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="delay">Optional delivery delay.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync<T>(
        string queueName,
        T message,
        TimeSpan? delay = null,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Receives a batch of messages from the queue.
    /// </summary>
    /// <typeparam name="T">Message payload type.</typeparam>
    /// <param name="queueName">Queue name.</param>
    /// <param name="maxMessages">Maximum number of messages to receive in one call.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of received cloud messages. Empty if the queue is empty.</returns>
    Task<IReadOnlyList<CloudMessage<T>>> ReceiveAsync<T>(
        string queueName,
        int maxMessages = 10,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Acknowledges (deletes) a message after successful processing.
    /// </summary>
    /// <param name="queueName">Queue name.</param>
    /// <param name="receiptHandle">Provider-specific message receipt handle from <see cref="CloudMessage{T}"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AcknowledgeAsync(string queueName, string receiptHandle, CancellationToken ct = default);
}

/// <summary>
/// A received cloud queue message with its payload and provider-specific receipt handle.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public sealed record CloudMessage<T>(
    T Payload,
    string MessageId,
    string ReceiptHandle,
    DateTimeOffset EnqueuedAt,
    int DeliveryCount);
