namespace Templates.Domain.Common;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier</typeparam>
public abstract class AggregateRoot<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the aggregate's unique identifier.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Gets the domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate's event collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    /// <exception cref="ArgumentNullException">Thrown when domainEvent is null</exception>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));
            
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the aggregate.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

