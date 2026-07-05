namespace Templates.Tests.Unit.Domain.Orders;

using FluentAssertions;
using Xunit;
using Templates.Domain.Orders;

/// <summary>
/// Unit tests for Order aggregate root.
/// </summary>
public class OrderTests
{
    [Fact]
    public void Create_WithValidInput_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            OrderItem.Create(
                ProductId.Create(Guid.NewGuid()),
                2,
                Money.Create(99.99m))
        };

        // Act
        var order = Order.Create(customerId, items);

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().NotBe(OrderId.Create(Guid.Empty));
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Amount.Should().Be(199.98m);
        order.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var items = new List<OrderItem> { OrderItem.Create(ProductId.Create(Guid.NewGuid()), 1, Money.Create(100)) };

        // Act & Assert
        var action = () => Order.Create(Guid.Empty, items);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyItems_ThrowsArgumentException()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act & Assert
        var action = () => Order.Create(customerId, new List<OrderItem>());
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Confirm_WithPendingOrder_ConfirmsOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var items = new List<OrderItem> { OrderItem.Create(ProductId.Create(Guid.NewGuid()), 1, Money.Create(100)) };
        var order = Order.Create(customerId, items);
        order.ClearDomainEvents();

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.UpdatedAt.Should().NotBeNull();
        order.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Confirm_WithConfirmedOrder_ThrowsOrderDomainException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var items = new List<OrderItem> { OrderItem.Create(ProductId.Create(Guid.NewGuid()), 1, Money.Create(100)) };
        var order = Order.Create(customerId, items);
        order.Confirm();

        // Act & Assert
        var action = () => order.Confirm();
        action.Should().Throw<OrderDomainException>()
            .WithMessage("Only pending orders can be confirmed");
    }

    [Fact]
    public void Cancel_WithPendingOrder_CancelsOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var items = new List<OrderItem> { OrderItem.Create(ProductId.Create(Guid.NewGuid()), 1, Money.Create(100)) };
        var order = Order.Create(customerId, items);
        order.ClearDomainEvents();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }
}
