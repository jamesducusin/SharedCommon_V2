namespace Templates.Application.Features.Orders.Create;

using MediatR;
using SharedCommon.Core;

/// <summary>
/// Command to create a new order.
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemDto> Items) : IRequest<Result<CreateOrderResponse>>;

/// <summary>
/// Order item DTO for create order command.
/// </summary>
public sealed record CreateOrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Response for successful order creation.
/// </summary>
public sealed record CreateOrderResponse(
    Guid OrderId,
    decimal TotalAmount,
    DateTime CreatedAt);
