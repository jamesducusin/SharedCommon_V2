namespace Templates.Domain.Common;

/// <summary>
/// Represents a domain event raised by an aggregate when its state changes.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the date and time when this event occurred.
    /// </summary>
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }

    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
}
