namespace Templates.Tests.Unit.Application.Features.Orders.Create;

using FluentAssertions;
using Moq;
using Xunit;
using MediatR;
using Microsoft.Extensions.Logging;
using Templates.Application.Features.Orders.Create;
using Templates.Domain.Orders;
using Templates.Infrastructure.Common;

/// <summary>
/// Unit tests for CreateOrderCommandHandler.
/// </summary>
public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            customerId,
            new List<CreateOrderItemDto>
            {
                new(productId, 2, 99.99m)
            });

        var mockRepository = new Mock<IOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockPublisher = new Mock<IPublisher>();
        var mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();

        var handler = new CreateOrderCommandHandler(
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockPublisher.Object,
            mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().NotBe(Guid.Empty);
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.TotalAmount.Should().Be(199.98m);

        mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyCustomerId_ReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand(
            Guid.Empty,
            new List<CreateOrderItemDto> { new(Guid.NewGuid(), 1, 100) });

        var handler = new CreateOrderCommandHandler(
            new Mock<IOrderRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IPublisher>().Object,
            new Mock<ILogger<CreateOrderCommandHandler>>().Object);

        // Act & Assert
        var action = () => handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentException>();
    }
}
