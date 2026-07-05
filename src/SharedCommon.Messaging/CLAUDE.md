# SharedCommon.Messaging

Async messaging abstractions over Kafka/RabbitMQ via MassTransit.
Outbox pattern, idempotent consumers, dead letter queues.

## API Surface

- `IMessagePublisher` — publish domain events
- `IMessageConsumer<T>` — base for consumer implementations
- `MessagingOptions` — broker config, retry policy, dead letter
- `AddSharedMessaging(IConfiguration)` — DI + MassTransit registration
- `OutboxMessage` — transactional outbox support

## Rules

**Must:**
- Consumer implementations must be idempotent (safe to re-process)
- CorrelationId propagated in message headers
- Dead letter queue configured for all consumers
- Retry with exponential backoff (3 retries, then DLQ)
- Message contracts versioned (v1, v2 namespaces)
- Log processing start and end with MessageId

**Forbidden:**
- Fire-and-forget without exception handling
- Business logic in consumer error handlers
- Consuming own-published events (unless explicitly designed)
- Hardcoded broker addresses

## Design Decisions

Outbox pattern ensures at-least-once delivery without dual-write risk.
Consumer idempotency + at-least-once = effectively exactly-once.

## Test Strategy

- Unit test consumer logic with mock `ConsumeContext`
- Integration tests use in-memory MassTransit test harness
- Test DLQ routing when consumer throws

## Extension Points

- Custom `IMessageSerializer` for non-JSON message formats
- Custom `IConsumerObserver` for cross-cutting consumer concerns
