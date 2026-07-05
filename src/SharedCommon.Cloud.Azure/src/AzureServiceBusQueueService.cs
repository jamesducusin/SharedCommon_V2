using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace SharedCommon.Cloud.Azure;

/// <summary>
/// Azure Service Bus implementation of <see cref="ICloudQueueService"/>.
/// Uses <see cref="ServiceBusReceiveMode.ReceiveAndDelete"/> so that
/// <see cref="AcknowledgeAsync"/> is a no-op — messages are deleted on receipt.
/// </summary>
/// <remarks>
/// For scenarios requiring re-processing on failure, implement a custom
/// <see cref="ICloudQueueService"/> using PeekLock mode with explicit lock renewal.
/// </remarks>
internal sealed class AzureServiceBusQueueService : ICloudQueueService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusQueueService> _logger;

    public AzureServiceBusQueueService(IOptions<CloudOptions> options, ILogger<AzureServiceBusQueueService> logger)
    {
        _logger = logger;
        var azure = options.Value.Azure;
        var ns = azure.ServiceBusNamespace
            ?? throw new InvalidOperationException(
                "CloudOptions:Azure:ServiceBusNamespace is required for ICloudQueueService.");

        _client = azure.UseManagedIdentity
            ? new ServiceBusClient(ns, new DefaultAzureCredential())
            : new ServiceBusClient(ns);
    }

    /// <inheritdoc/>
    public async Task SendAsync<T>(
        string queueName,
        T message,
        TimeSpan? delay = null,
        CancellationToken ct = default)
        where T : class
    {
        await using var sender = _client.CreateSender(queueName);
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var sbMessage = new ServiceBusMessage(body) { ContentType = "application/json" };

        if (delay.HasValue)
        {
            sbMessage.ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(delay.Value);
            await sender.ScheduleMessageAsync(sbMessage, sbMessage.ScheduledEnqueueTime, ct);
        }
        else
        {
            await sender.SendMessageAsync(sbMessage, ct);
        }

        _logger.LogDebug("Sent message to Service Bus queue {Queue}", queueName);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CloudMessage<T>>> ReceiveAsync<T>(
        string queueName,
        int maxMessages = 10,
        CancellationToken ct = default)
        where T : class
    {
        await using var receiver = _client.CreateReceiver(
            queueName,
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

        var messages = await receiver.ReceiveMessagesAsync(maxMessages, cancellationToken: ct);

        return messages
            .Select(m => new CloudMessage<T>(
                Payload: JsonSerializer.Deserialize<T>(m.Body.ToArray())!,
                MessageId: m.MessageId,
                ReceiptHandle: m.MessageId,
                EnqueuedAt: m.EnqueuedTime,
                DeliveryCount: m.DeliveryCount))
            .ToList();
    }

    /// <inheritdoc/>
    /// <remarks>No-op: messages are deleted on receipt in <see cref="ServiceBusReceiveMode.ReceiveAndDelete"/> mode.</remarks>
    public Task AcknowledgeAsync(string queueName, string receiptHandle, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}
