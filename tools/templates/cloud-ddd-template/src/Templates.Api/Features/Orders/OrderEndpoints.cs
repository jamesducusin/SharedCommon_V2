namespace Templates.Api.Features.Orders;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedCommon.ResponseBuilder;
using Templates.Application.Features.Orders.Create;

/// <summary>
/// Endpoints for order management.
/// </summary>
public static class OrderEndpoints
{
    /// <summary>
    /// Maps order endpoints to the application.
    /// </summary>
    public static void MapOrderFeatures(this WebApplication app)
    {
        var group = app.MapGroup("/orders")
            .WithName("Orders")
            .WithOpenApi()
            .WithTags("Orders");

        group.MapPost("", CreateOrder)
            .WithName("CreateOrder")
            .WithSummary("Create a new order")
            .Produces<ApiResponse<CreateOrderResponse>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{orderId}", GetOrder)
            .WithName("GetOrder")
            .WithSummary("Retrieve an order by ID")
            .Produces<ApiResponse<OrderResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("", ListOrders)
            .WithName("ListOrders")
            .WithSummary("List orders with pagination")
            .Produces<ApiResponse<PagedOrdersResponse>>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    private static async Task<IResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        ISender sender,
        IResponseBuilder responseBuilder,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? responseBuilder.Success(result.Value)
                .WithStatusCode(StatusCodes.Status201Created)
                .Build()
            : responseBuilder.Failure(result.Error)
                .Build();
    }

    /// <summary>
    /// Retrieves an order by ID.
    /// </summary>
    private static async Task<IResult> GetOrder(
        [FromRoute] Guid orderId,
        IResponseBuilder responseBuilder)
    {
        // TODO: Implement get order query
        return responseBuilder.Success(new OrderResponse(
            orderId,
            Guid.NewGuid(),
            0,
            "Pending",
            DateTime.UtcNow))
            .Build();
    }

    /// <summary>
    /// Lists orders with pagination.
    /// </summary>
    private static async Task<IResult> ListOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        IResponseBuilder responseBuilder)
    {
        // TODO: Implement list orders query
        return responseBuilder.Success(new PagedOrdersResponse(
            new List<OrderResponse>(),
            pageNumber,
            pageSize,
            0,
            0))
            .Build();
    }
}

/// <summary>
/// Order response DTO.
/// </summary>
public sealed record OrderResponse(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);

/// <summary>
/// Paged orders response DTO.
/// </summary>
public sealed record PagedOrdersResponse(
    List<OrderResponse> Orders,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
