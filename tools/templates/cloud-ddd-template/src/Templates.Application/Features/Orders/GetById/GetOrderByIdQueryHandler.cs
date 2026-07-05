using MediatR;
using Microsoft.Extensions.Logging;
using SharedCommon.Core;
using Templates.Domain.Orders;
using Templates.Infrastructure.Persistence.Dapper;

namespace Templates.Application.Features.Orders.GetById;

/// <summary>
/// Handler for GetOrderByIdQuery using Dapper stored procedures.
/// Demonstrates the convenient abstraction layer for read operations.
/// </summary>
public sealed class GetOrderByIdQueryHandler(
    IDapperRepository<Order, OrderId> orderRepository,
    ILogger<GetOrderByIdQueryHandler> logger) : IRequestHandler<GetOrderByIdQuery, Result<GetOrderByIdResponse>>
{
    /// <summary>
    /// Handles the get order by ID query.
    /// Uses sp_Order_GetById stored procedure via generic repository.
    /// </summary>
    public async Task<Result<GetOrderByIdResponse>> Handle(
        GetOrderByIdQuery request,
        CancellationToken ct)
    {
        Guard.NotNull(request, nameof(request));
        Guard.AgainstEmptyGuid(request.OrderId, nameof(request.OrderId));

        try
        {
            logger.LogInformation(
                "Retrieving order with ID {OrderId}",
                request.OrderId);

            // Automatic use of sp_Order_GetById - no stored procedure name needed!
            // The generic repository uses naming convention: sp_{EntityName}_GetById
            var order = await orderRepository.GetByIdAsync(
                new OrderId(request.OrderId),
                ct).ConfigureAwait(false);

            if (order == null)
            {
                logger.LogWarning(
                    "Order with ID {OrderId} not found",
                    request.OrderId);

                return Result.Failure<GetOrderByIdResponse>(
                    Error.NotFound("Order", request.OrderId.ToString()));
            }

            logger.LogInformation(
                "Order {OrderId} retrieved successfully",
                request.OrderId);

            var response = new GetOrderByIdResponse(
                order.Id.Value,
                order.CustomerId,
                order.Status.ToString(),
                order.TotalAmount.Amount,
                order.CreatedAt);

            return Result.Success(response);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation cancelled while retrieving order {OrderId}", request.OrderId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error retrieving order {OrderId}",
                request.OrderId);

            return Result.Failure<GetOrderByIdResponse>(
                Error.InternalServerError());
        }
    }
}
