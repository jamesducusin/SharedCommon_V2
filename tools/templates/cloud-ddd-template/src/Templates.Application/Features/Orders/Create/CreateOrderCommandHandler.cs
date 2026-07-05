namespace Templates.Application.Features.Orders.Create;

using MediatR;
using Microsoft.Extensions.Logging;
using SharedCommon.Core;
using Templates.Domain.Orders;
using Templates.Infrastructure.Persistence.Dapper;

/// <summary>
/// Handler for CreateOrderCommand using Dapper stored procedures.
/// Optimized for high-traffic scenarios (1000+ TPS).
/// </summary>
public sealed class CreateOrderCommandHandler(
    IDapperRepository<Order, OrderId> orderRepository,
    IStoredProcedureExecutor procedureExecutor,
    IPublisher publisher,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    /// <summary>
    /// Handles the create order command via stored procedures.
    /// </summary>
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Creating order for customer {CustomerId} with {ItemCount} items",
            request.CustomerId,
            request.Items.Count);

        // Validate input
        Guard.AgainstEmptyGuid(request.CustomerId, nameof(request.CustomerId));
        Guard.AgainstEmpty(request.Items, nameof(request.Items));

        try
        {
            // Create domain aggregate
            var items = request.Items
                .Select(i => OrderItem.Create(
                    ProductId.Create(i.ProductId),
                    i.Quantity,
                    Money.Create(i.UnitPrice)))
                .ToList();

            var order = Order.Create(request.CustomerId, items);

            // Persist using stored procedure (sp_Order_Insert)
            var parameters = new
            {
                OrderId = order.Id.Value,
                CustomerId = order.CustomerId,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount.Amount,
                CreatedAt = order.CreatedAt
            };

            var affectedRows = await orderRepository.ExecuteAsync(
                "sp_Order_Insert",
                parameters,
                ct).ConfigureAwait(false);

            if (affectedRows == 0)
            {
                logger.LogError("Failed to insert order {OrderId}", order.Id.Value);
                return Result.Failure<CreateOrderResponse>(
                    new Error("CreateFailed", "Failed to create order"));
            }

            // Insert order items in transaction (sp_OrderItem_Insert)
            await orderRepository.ExecuteTransactionAsync(
                async (connection, transaction) =>
                {
                    foreach (var item in order.Items)
                    {
                        var itemParams = new
                        {
                            OrderId = order.Id.Value,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice.Amount,
                            LineTotal = item.LineTotal.Amount
                        };

                        var itemResult = await connection.ExecuteAsync(
                            "sp_OrderItem_Insert",
                            itemParams,
                            transaction,
                            commandType: System.Data.CommandType.StoredProcedure,
                            commandTimeout: 30);

                        if (itemResult == 0)
                            throw new ApplicationException(
                                $"Failed to insert order item for product {item.ProductId}");
                    }

                    return affectedRows;
                },
                ct).ConfigureAwait(false);

            // Publish domain events
            foreach (var @event in order.DomainEvents)
            {
                await publisher.Publish(@event, ct).ConfigureAwait(false);
            }

            order.ClearDomainEvents();

            logger.LogInformation(
                "Order {OrderId} created successfully for customer {CustomerId}",
                order.Id.Value,
                order.CustomerId);

            return Result.Success(new CreateOrderResponse(
                order.Id.Value,
                order.TotalAmount.Amount,
                order.CreatedAt));
        }
        catch (OrderDomainException ex)
        {
            logger.LogWarning(
                ex,
                "Order creation failed with domain exception: {ErrorCode} - {Message}",
                ex.Code,
                ex.Message);

            return Result.Failure<CreateOrderResponse>(
                Error.FromException(ex));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error creating order for customer {CustomerId}",
                request.CustomerId);

            throw;
        }
    }
}
