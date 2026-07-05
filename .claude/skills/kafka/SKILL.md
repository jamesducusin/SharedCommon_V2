# Kafka Skill

Implement async messaging with Kafka and MassTransit.

## When to Use This Skill

Triggers:
- Publishing domain events
- Consuming messages
- Designing message contracts
- Configuring retry and dead letter

Ask Claude explicitly: "Use kafka skill"

## Checklist

- [ ] Message contracts in separate assembly (versioned)
- [ ] Producer uses outbox pattern for at-least-once delivery
- [ ] Consumer is idempotent (safe to re-process)
- [ ] Dead letter queue configured
- [ ] Retry policy with exponential backoff
- [ ] Message schema versioning strategy documented
- [ ] Consumer group ID stable and meaningful
- [ ] Correlation ID propagated in message headers
- [ ] Consumer logs processing start/end with message key

## References

See: src/SharedCommon.Messaging/CLAUDE.md
See: samples/Sample.Kafka/
See: docs/adr/ (messaging decisions)
