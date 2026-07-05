# SharedCommon.Messaging

Async messaging over **RabbitMQ or Apache Kafka** via MassTransit. Choose your transport via configuration — the application code (publishers and consumers) is the same for both. Includes `IMessagePublisher`, a pre-configured consumer pipeline with retry, dead-letter queuing, correlation ID propagation, and structured logging.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Messaging
```

## Registration

```csharp
builder.Services.AddSharedMessaging(builder.Configuration, bus =>
{
    // Register your consumers here — same code regardless of transport
    bus.AddConsumer<OrderCreatedConsumer>();
    bus.AddConsumer<PaymentProcessedConsumer>();
});
```

## Choosing a Transport

Set `Transport` in configuration. All other code stays the same.

```json
{
  "SharedCommon": {
    "Messaging": {
      "Transport": "RabbitMQ"
    }
  }
}
```

| Value | When to use |
|-------|-------------|
| `RabbitMQ` (default) | General-purpose work queues, request/reply, task distribution |
| `Kafka` | High-throughput event streaming, log-based messaging, event sourcing |

---

## RabbitMQ Configuration

Used when `Transport` is `RabbitMQ` (the default).

```json
{
  "SharedCommon": {
    "Messaging": {
      "Transport": "RabbitMQ",
      "RabbitMQ": {
        "Host": "rabbitmq",
        "Port": 5672,
        "VirtualHost": "/",
        "Username": "guest"
      },
      "Retry": {
        "MaxAttempts": 3,
        "MinInterval": "00:00:01",
        "MaxInterval": "00:00:30",
        "IntervalDelta": "00:00:02"
      }
    }
  }
}
```

> **Password** must never be in `appsettings.json`. Use User Secrets or your secrets manager:
> ```bash
> dotnet user-secrets set "SharedCommon:Messaging:RabbitMQ:Password" "rabbitmq-password"
> ```

| Property | Default | Notes |
|----------|---------|-------|
| `RabbitMQ.Host` | `localhost` | RabbitMQ hostname or IP. |
| `RabbitMQ.Port` | `5672` | AMQP port. |
| `RabbitMQ.VirtualHost` | `/` | RabbitMQ virtual host. |
| `RabbitMQ.Username` | `guest` | Use a non-guest user in production. |
| `Retry.MaxAttempts` | `3` | After MaxAttempts, the message routes to the dead-letter queue. |
| `Retry.MinInterval` | `1s` | Minimum back-off delay. |
| `Retry.MaxInterval` | `30s` | Maximum back-off delay. |

---

## Kafka Configuration

Used when `Transport` is `Kafka`.

```json
{
  "SharedCommon": {
    "Messaging": {
      "Transport": "Kafka",
      "Kafka": {
        "BootstrapServers": "broker1:9092,broker2:9092",
        "ConsumerGroupId": "order-service",
        "SecurityProtocol": "SaslSsl"
      },
      "Retry": {
        "MaxAttempts": 3
      }
    }
  }
}
```

> **SASL credentials** must never be in `appsettings.json`:
> ```bash
> dotnet user-secrets set "SharedCommon:Messaging:Kafka:SaslUsername" "kafka-user"
> dotnet user-secrets set "SharedCommon:Messaging:Kafka:SaslPassword" "kafka-password"
> ```

| Property | Default | Notes |
|----------|---------|-------|
| `Kafka.BootstrapServers` | `localhost:9092` | Comma-separated `host:port` list. |
| `Kafka.ConsumerGroupId` | `shared-common` | All instances of your service share one group. |
| `Kafka.SecurityProtocol` | `Plaintext` | Use `SaslSsl` in production. |
| `Kafka.DefaultTopicPartitions` | `1` | For auto-created topics. |
| `Kafka.DefaultTopicReplicationFactor` | `1` | Must be ≤ broker count. |

---

## Publishing Events

Inject `IMessagePublisher` into any service — identical code for both transports:

```csharp
public class OrderService(IMessagePublisher publisher)
{
    public async Task CreateAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.CreateAsync(cmd, ct);

        await publisher.PublishAsync(new OrderCreatedEvent
        {
            OrderId    = order.Id,
            CustomerId = order.CustomerId,
            Total      = order.Total,
            CreatedAt  = order.CreatedAt
        }, ct);
    }
}
```

With additional MassTransit publish configuration:

```csharp
await publisher.PublishAsync(new OrderCreatedEvent { ... },
    configure: ctx => ctx.SetPriority(5),
    ct);
```

## Consuming Events

Implement `IConsumer<T>` and register in `AddSharedMessaging` — identical code for both transports:

```csharp
public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}",
            msg.OrderId, msg.CustomerId);

        await _notificationService.SendConfirmationAsync(msg.CustomerId, msg.OrderId);
    }
}
```

### Consumer rules

- Consumers **must be idempotent** — at-least-once delivery means a message may arrive more than once.
- Do not put business logic in error handlers.
- Do not consume events your own service published (unless explicitly designed for it).

## Retry and Dead-Letter Queue

The pipeline is pre-wired automatically for both transports:

1. Message arrives → consumer processes it
2. Consumer throws → exponential back-off retry (up to `Retry.MaxAttempts`)
3. All retries exhausted → message routes to the dead-letter queue (`{exchange}_error` on RabbitMQ, a separate error topic on Kafka)

## Message Contract Versioning

Namespace message contracts by version to avoid breaking consumers during upgrades:

```csharp
namespace MyService.Contracts.V1
{
    public record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset CreatedAt);
}

namespace MyService.Contracts.V2
{
    // Added ShippingAddress — V1 consumers continue to work unaffected
    public record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset CreatedAt, string ShippingAddress);
}
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IMessagePublisher` | Scoped | Publishes via the MassTransit `IBus`. |
| MassTransit `IBus` | Singleton | Backed by the configured transport. |
| `CorrelationIdFilter<T>` | — | Applied globally to all consumers. |
| `LoggingFilter<T>` | — | Applied globally to all consumers. |
