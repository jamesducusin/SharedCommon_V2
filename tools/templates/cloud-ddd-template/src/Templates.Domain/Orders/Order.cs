namespace Templates.Domain.Orders;

using Templates.Domain.Common;

/// <summary>
/// Value object representing an order ID.
/// </summary>
public sealed record OrderId(Guid Value)
{
    /// <summary>
    /// Creates a new order ID.
    /// </summary>
    public static OrderId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an order ID from a GUID.
    /// </summary>
    public static OrderId Create(Guid value)
    {
        Guard.AgainstEmptyGuid(value, nameof(value));
        return new OrderId(value);
    }
}

/// <summary>
/// Value object representing a product ID.
/// </summary>
public sealed record ProductId(Guid Value)
{
    /// <summary>
    /// Creates a product ID from a GUID.
    /// </summary>
    public static ProductId Create(Guid value)
    {
        Guard.AgainstEmptyGuid(value, nameof(value));
        return new ProductId(value);
    }
}

/// <summary>
/// Value object representing money.
/// </summary>
public sealed record Money(decimal Amount)
{
    /// <summary>
    /// Creates money from a decimal amount.
    /// </summary>
    public static Money Create(decimal amount)
    {
        Guard.AgainstLessThan(amount, 0, nameof(amount));
        return new Money(amount);
    }

    public static Money operator *(Money money, int quantity) => new(money.Amount * quantity);
    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
}

/// <summary>
/// Value object representing an order item.
/// </summary>
public sealed record OrderItem(ProductId ProductId, int Quantity, Money UnitPrice)
{
    /// <summary>
    /// Creates an order item.
    /// </summary>
    public static OrderItem Create(ProductId productId, int quantity, Money unitPrice)
    {
        Guard.AgainstNull(productId, nameof(productId));
        Guard.AgainstLessThan(quantity, 1, nameof(quantity));
        Guard.AgainstNull(unitPrice, nameof(unitPrice));
        return new OrderItem(productId, quantity, unitPrice);
    }

    /// <summary>
    /// Gets the total price for this order item.
    /// </summary>
    public Money LineTotal => UnitPrice * Quantity;
}

/// <summary>
/// Order entity — aggregate root for the Orders bounded context.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = [];

    /// <summary>
    /// Gets the customer ID.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the order items.
    /// </summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets the order status.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Gets the order total.
    /// </summary>
    public Money TotalAmount { get; private set; } = Money.Create(0);

    /// <summary>
    /// Gets the date the order was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date the order was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private Order() { }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    public static Order Create(Guid customerId, List<OrderItem> items)
    {
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Guard.AgainstEmpty(items, nameof(items));

        var order = new Order
        {
            Id = OrderId.New(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order._items.AddRange(items);
        order.CalculateTotalAmount();

        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id.Value, customerId, order.TotalAmount.Amount));

        return order;
    }

    /// <summary>
    /// Confirms the order.
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException("Only pending orders can be confirmed", "Order.InvalidStatus", 400);

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderConfirmedDomainEvent(Id.Value));
    }

    /// <summary>
    /// Cancels the order.
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new OrderDomainException("Cannot cancel a shipped or delivered order", "Order.InvalidStatus", 400);

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderCancelledDomainEvent(Id.Value));
    }

    private void CalculateTotalAmount()
    {
        if (_items.Count == 0)
        {
            TotalAmount = Money.Create(0);
            return;
        }

        TotalAmount = _items.Aggregate(Money.Create(0), (sum, item) => sum + item.LineTotal);
    }
}

/// <summary>
/// Order status enum.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

/// <summary>
/// Exception for order domain errors.
/// </summary>
public sealed class OrderDomainException : DomainException
{
    public OrderDomainException(string message, string code = "Order.Error", int statusCode = 400)
        : base(message, code, statusCode)
    {
    }
}
