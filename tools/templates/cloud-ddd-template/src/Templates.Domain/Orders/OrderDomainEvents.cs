namespace Templates.Domain.Orders;

using Templates.Domain.Common;

/// <summary>
/// Domain event raised when an order is created.
/// </summary>
public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount) : DomainEvent;

/// <summary>
/// Domain event raised when an order is confirmed.
/// </summary>
public sealed record OrderConfirmedDomainEvent(Guid OrderId) : DomainEvent;

/// <summary>
/// Domain event raised when an order is cancelled.
/// </summary>
public sealed record OrderCancelledDomainEvent(Guid OrderId) : DomainEvent;
