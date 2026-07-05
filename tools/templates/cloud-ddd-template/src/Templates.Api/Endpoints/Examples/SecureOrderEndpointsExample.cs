namespace Templates.Api.Endpoints.Examples;

/// <summary>
/// Example endpoints showing how to implement security, authentication, and error handling
/// using the new Phase 1 features.
/// 
/// This file demonstrates best practices and can be used as a template for your own endpoints.
/// </summary>
public static class SecureOrderEndpointsExample
{
    /// <summary>
    /// Maps example order endpoints to the application.
    /// </summary>
    public static void MapSecureOrderEndpointsExample(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/orders")
            .WithName("Orders")
            .WithOpenApi()
            .WithTags("Orders");

        // PUBLIC: Get order by ID (no auth required)
        group.MapGet("/{id:guid}", GetOrderById)
            .WithName("GetOrderById")
            .WithSummary("Get a specific order")
            .Produces<GetOrderByIdResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithoutAuthorization();

        // PROTECTED: List orders (requires authentication)
        group.MapGet("", ListOrders)
            .WithName("ListOrders")
            .WithSummary("List all orders (requires authentication)")
            .Produces<List<OrderDto>>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        // PROTECTED: Create order (requires admin role)
        group.MapPost("", CreateOrder)
            .WithName("CreateOrder")
            .WithSummary("Create a new order (admin only)")
            .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "OrderManager"));

        // PROTECTED: Update order (requires admin role)
        group.MapPut("/{id:guid}", UpdateOrder)
            .WithName("UpdateOrder")
            .WithSummary("Update an existing order (admin only)")
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // PROTECTED: Delete order (requires admin role)
        group.MapDelete("/{id:guid}", DeleteOrder)
            .WithName("DeleteOrder")
            .WithSummary("Delete an order (admin only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    /// <summary>
    /// Example: Get order by ID - demonstrates handling EntityNotFoundException
    /// </summary>
    private static async Task<IResult> GetOrderById(
        Guid id,
        // Inject your repository
        // IOrderRepository orderRepo,
        CancellationToken ct)
    {
        // Simulating repository call
        OrderDto? order = null; // = await orderRepo.GetByIdAsync(id, ct);

        // Throw domain exception instead of returning NotFound()
        if (order == null)
            throw new EntityNotFoundException(nameof(Order), id);
            // Exception handler will return:
            // {
            //   "traceId": "...",
            //   "statusCode": 404,
            //   "error": {
            //     "code": "ENTITY_NOT_FOUND",
            //     "message": "Order with ID '...' was not found",
            //     "details": { "entityType": "Order", "entityId": "..." }
            //   }
            // }

        return Results.Ok(new GetOrderByIdResponse(order));
    }

    /// <summary>
    /// Example: List orders - demonstrates authentication requirement
    /// </summary>
    private static async Task<IResult> ListOrders(
        // IOrderRepository orderRepo,
        CancellationToken ct)
    {
        // This endpoint requires authentication via .RequireAuthorization()
        // If called without valid JWT token:
        // {
        //   "traceId": "...",
        //   "statusCode": 401,
        //   "error": {
        //     "code": "UNAUTHORIZED",
        //     "message": "Unauthorized access"
        //   }
        // }

        // var orders = await orderRepo.ListAsync(ct);
        // return Results.Ok(orders);

        return Results.Ok(new List<OrderDto>());
    }

    /// <summary>
    /// Example: Create order - demonstrates business rule validation
    /// </summary>
    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request,
        // ICommandHandler<CreateOrderCommand> handler,
        CancellationToken ct)
    {
        // Validate request structure (automatic via FluentValidation)
        // If validation fails, exception handler returns:
        // {
        //   "traceId": "...",
        //   "statusCode": 400,
        //   "error": {
        //     "code": "VALIDATION_FAILED",
        //     "message": "One or more validation errors occurred",
        //     "details": {
        //       "validationErrors": {
        //         "customerId": ["Customer ID is required"],
        //         "items": ["At least one item is required"]
        //       }
        //     }
        //   }
        // }

        // Simulate checking business rules
        if (request.Items == null || request.Items.Count == 0)
            throw new BusinessRuleViolationException(
                message: "Order must contain at least one item",
                ruleCode: "EMPTY_ORDER");
                // Exception handler returns:
                // {
                //   "traceId": "...",
                //   "statusCode": 400,
                //   "error": {
                //     "code": "BUSINESS_RULE_VIOLATION",
                //     "message": "Order must contain at least one item",
                //     "details": { "ruleCode": "EMPTY_ORDER" }
                //   }
                // }

        // Create order...
        // var result = await handler.Handle(new CreateOrderCommand(...), ct);

        return Results.Created($"/api/v1/orders/new-id", new CreateOrderResponse { Id = Guid.NewGuid() });
    }

    /// <summary>
    /// Example: Update order - demonstrates conflict handling
    /// </summary>
    private static async Task<IResult> UpdateOrder(
        Guid id,
        UpdateOrderRequest request,
        // IOrderRepository orderRepo,
        CancellationToken ct)
    {
        // Check if order exists
        OrderDto? order = null; // = await orderRepo.GetByIdAsync(id, ct);

        if (order == null)
            throw new EntityNotFoundException(nameof(Order), id);

        // Check if order is in a state where it can be updated
        if (order.Status == "Shipped")
            throw new ConflictException(
                message: "Cannot update an order that has already been shipped",
                details: new()
                {
                    { "currentStatus", "Shipped" },
                    { "orderId", id }
                });
                // Exception handler returns:
                // {
                //   "traceId": "...",
                //   "statusCode": 409,
                //   "error": {
                //     "code": "CONFLICT",
                //     "message": "Cannot update an order that has already been shipped",
                //     "details": { "currentStatus": "Shipped", "orderId": "..." }
                //   }
                // }

        // Update order...
        // order = order with { ... request properties };
        // await orderRepo.UpdateAsync(order, ct);

        return Results.Ok(order);
    }

    /// <summary>
    /// Example: Delete order - demonstrates cascading authorization
    /// </summary>
    private static async Task<IResult> DeleteOrder(
        Guid id,
        // IOrderRepository orderRepo,
        CancellationToken ct)
    {
        // Check if order exists
        OrderDto? order = null; // = await orderRepo.GetByIdAsync(id, ct);

        if (order == null)
            throw new EntityNotFoundException(nameof(Order), id);

        // Delete order...
        // await orderRepo.DeleteAsync(id, ct);

        return Results.NoContent();
    }
}

// ============================================================================
// EXAMPLE DTOs AND MODELS
// ============================================================================

/// <summary>
/// Represents an order entity.
/// </summary>
public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    List<OrderItemDto> Items);

/// <summary>
/// Represents an item within an order.
/// </summary>
public record OrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Request model for creating an order.
/// </summary>
public record CreateOrderRequest(
    Guid CustomerId,
    List<CreateOrderItemRequest> Items);

/// <summary>
/// Request model for an order item.
/// </summary>
public record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity);

/// <summary>
/// Response model for created order.
/// </summary>
public record CreateOrderResponse(Guid Id);

/// <summary>
/// Request model for updating an order.
/// </summary>
public record UpdateOrderRequest(
    string Status,
    List<UpdateOrderItemRequest>? Items = null);

/// <summary>
/// Request model for updating an order item.
/// </summary>
public record UpdateOrderItemRequest(
    Guid ProductId,
    int Quantity);

/// <summary>
/// Response model for get order by ID.
/// </summary>
public record GetOrderByIdResponse(OrderDto? Order);

// ============================================================================
// PLACEHOLDER TYPES (remove if already defined)
// ============================================================================

public class Order { }
public class OrderItem { }

using Templates.Api.Common.Models;
using Templates.Domain.Common.Exceptions;
