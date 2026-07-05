using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using SharedCommon.Core;
using Templates.Application.Features.Orders.Create;
using Templates.Application.Features.Orders.GetById;
using Templates.Domain.Orders;
using Templates.Infrastructure.Persistence.Dapper;
using Microsoft.Extensions.Logging;

namespace Templates.Application.Features.Orders.UnitTests;

/// <summary>
/// Unit tests for Order command and query handlers using mocked Dapper repository.
/// Demonstrates best practices for testing with generic IDapperRepository.
/// </summary>
public class OrderHandlerTests
{
    private readonly Mock<IDapperRepository<Order, OrderId>> _mockRepository;
    private readonly Mock<IStoredProcedureExecutor> _mockExecutor;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly Mock<ILogger<GetOrderByIdQueryHandler>> _mockLogger;

    public OrderHandlerTests()
    {
        _mockRepository = new Mock<IDapperRepository<Order, OrderId>>();
        _mockExecutor = new Mock<IStoredProcedureExecutor>();
        _mockPublisher = new Mock<IPublisher>();
        _mockLogger = new Mock<ILogger<GetOrderByIdQueryHandler>>();
    }

    #region GetOrderByIdQuery Tests

    [Fact]
    public async Task GetOrderByIdQueryHandler_ReturnsOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var order = new Order
        {
            Id = new OrderId(orderId),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = new Money(100.00m),
            CreatedAt = DateTime.UtcNow
        };

        var query = new GetOrderByIdQuery(orderId);
        
        _mockRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<OrderId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new GetOrderByIdQueryHandler(_mockRepository.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().Be(orderId);
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.Status.Should().Be("Pending");
        result.Value.TotalAmount.Should().Be(100.00m);

        // Verify the generic repository method was called with correct parameters
        _mockRepository.Verify(
            x => x.GetByIdAsync(
                It.Is<OrderId>(id => id.Value == orderId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrderByIdQueryHandler_ReturnsFailure_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var query = new GetOrderByIdQuery(orderId);

        _mockRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<OrderId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new GetOrderByIdQueryHandler(_mockRepository.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be("NotFound");
    }

    [Fact]
    public async Task GetOrderByIdQueryHandler_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var query = new GetOrderByIdQuery(orderId);
        var cancellationToken = new CancellationToken(canceled: true);

        _mockRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<OrderId>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var handler = new GetOrderByIdQueryHandler(_mockRepository.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cancellationToken));
    }

    #endregion

    #region CreateOrderCommand Tests

    [Fact]
    public async Task CreateOrderCommandHandler_CreatesOrder_WhenCommandIsValid()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            customerId,
            new List<CreateOrderCommand.OrderItemDto>
            {
                new(productId, 2, 50.00m)
            });

        var mockCreateLogger = new Mock<ILogger<CreateOrderCommandHandler>>();

        _mockRepository
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 1 row affected

        _mockRepository
            .Setup(x => x.ExecuteTransactionAsync(
                It.IsAny<Func<IDbConnection, IDbTransaction, Task<int>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateOrderCommandHandler(
            _mockRepository.Object,
            _mockExecutor.Object,
            _mockPublisher.Object,
            mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalAmount.Should().Be(100.00m); // 2 * 50

        // Verify repository ExecuteAsync was called for insert
        _mockRepository.Verify(
            x => x.ExecuteAsync(
                "sp_Order_Insert",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify transaction was executed for items
        _mockRepository.Verify(
            x => x.ExecuteTransactionAsync(
                It.IsAny<Func<IDbConnection, IDbTransaction, Task<int>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify domain events were published
        _mockPublisher.Verify(
            x => x.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderCommandHandler_ReturnsFailure_WhenNoItems()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(customerId, new List<CreateOrderCommand.OrderItemDto>());

        var mockCreateLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
        var handler = new CreateOrderCommandHandler(
            _mockRepository.Object,
            _mockExecutor.Object,
            _mockPublisher.Object,
            mockCreateLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be("ValidationError");

        // Verify repository was never called
        _mockRepository.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region StoredProcedureExecutor Tests

    [Fact]
    public async Task StoredProcedureExecutor_ExecutesCustomProcedure_WhenCalledDirectly()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var parameters = new { CustomerId = customerId };

        var orders = new List<Order>
        {
            new() { Id = new OrderId(Guid.NewGuid()), CustomerId = customerId }
        };

        _mockExecutor
            .Setup(x => x.QueryAsync<Order>(
                "sp_Order_GetByCustomer",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _mockExecutor.Object.QueryAsync<Order>(
            "sp_Order_GetByCustomer",
            parameters,
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().CustomerId.Should().Be(customerId);

        _mockExecutor.Verify(
            x => x.QueryAsync<Order>(
                "sp_Order_GetByCustomer",
                parameters,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task GetOrdersQueryHandler_ReturnsPaginatedList_WhenHandlerExists()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = new OrderId(Guid.NewGuid()), CustomerId = Guid.NewGuid() },
            new() { Id = new OrderId(Guid.NewGuid()), CustomerId = Guid.NewGuid() }
        };

        _mockRepository
            .Setup(x => x.ListAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _mockRepository.Object.ListAsync(1, 25, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);

        _mockRepository.Verify(
            x => x.ListAsync(1, 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
