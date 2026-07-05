namespace SharedCommon.Messaging;

/// <summary>
/// Represents a message pending delivery in the transactional outbox.
/// Persisted in the same transaction as the domain write; a background worker
/// picks it up and publishes to the broker, ensuring at-least-once delivery
/// without dual-write risk.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Unique message identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Full CLR type name of the message contract (used to deserialize <see cref="MessageBody"/>).</summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>JSON-serialized message payload.</summary>
    public string MessageBody { get; init; } = string.Empty;

    /// <summary>Correlation ID from the originating request.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>When the message was written to the outbox.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the message was successfully published to the broker. <c>null</c> if not yet processed.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Number of failed publish attempts. Reset to zero on success.</summary>
    public int RetryCount { get; set; }

    /// <summary>Last error message if the most recent publish attempt failed.</summary>
    public string? LastError { get; set; }

    /// <summary>Returns <c>true</c> when the message has been successfully published.</summary>
    public bool IsProcessed => ProcessedAt.HasValue;
}
