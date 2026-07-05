namespace Templates.Application.Features.Orders.Create;

using Templates.Application.Common.Telemetry;
using Templates.Domain.Common.Exceptions;

/// <summary>
/// Example command handler showing how to use ITelemetryService for distributed tracing.
/// Demonstrates best practices for instrumenting business logic.
/// </summary>
public class CreateOrderCommandHandlerWithTelemetryExample
{
    private readonly ITelemetryService _telemetry;
    // private readonly IOrderRepository _repository;

    public CreateOrderCommandHandlerWithTelemetryExample(
        ITelemetryService telemetry)
        // IOrderRepository repository)
    {
        _telemetry = telemetry;
        // _repository = repository;
    }

    /// <summary>
    /// Handle order creation with full distributed tracing.
    /// Creates activities for the overall operation and sub-operations.
    /// Records metrics and logs exceptions with context.
    /// </summary>
    public async Task<OrderCreatedResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken ct)
    {
        // Start operation span
        using var operationScope = _telemetry.StartOperation("CreateOrder", "command");
        operationScope.SetTag("customer.id", command.CustomerId);
        operationScope.SetTag("order.items_count", command.Items.Count);

        try
        {
            // Validate business rules (example 1)
            ValidateOrderItems(command, operationScope);

            // Fetch customer (example 2)
            var customer = await FetchAndValidateCustomer(command, operationScope, ct);

            // Calculate total (example 3)
            var orderTotal = CalculateOrderTotal(command, operationScope);

            // Create order in database (example 4)
            var orderId = await CreateOrderInDatabase(
                command, customer, orderTotal, operationScope, ct);

            // Emit event (example 5)
            EmitOrderCreatedEvent(orderId, operationScope);

            // Mark operation as succeeded
            operationScope.MarkSucceeded();

            // Record metric
            _telemetry.RecordMetric("orders.created", 1, new()
            {
                { "customer_id", command.CustomerId },
                { "order_total", orderTotal }
            });

            return new OrderCreatedResult { OrderId = orderId };
        }
        catch (Exception ex)
        {
            operationScope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Example 1: Validate business rules with telemetry context.
    /// </summary>
    private void ValidateOrderItems(
        CreateOrderCommand command,
        IOperationScope operationScope)
    {
        using var scope = _telemetry.StartOperation("ValidateOrderItems", "validation");

        if (command.Items == null || command.Items.Count == 0)
        {
            scope.MarkFailed("Empty items list");
            throw new BusinessRuleViolationException(
                "Order must contain at least one item",
                "EMPTY_ORDER");
        }

        scope.SetTag("items_validated", command.Items.Count);
        scope.MarkSucceeded();
    }

    /// <summary>
    /// Example 2: Fetch and validate customer with telemetry context.
    /// </summary>
    private async Task<CustomerDto> FetchAndValidateCustomer(
        CreateOrderCommand command,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("FetchCustomer", "query");
        scope.SetTag("customer.id", command.CustomerId);

        try
        {
            // Simulate customer fetch
            // var customer = await _repository.GetCustomerAsync(command.CustomerId, ct);

            var customer = new CustomerDto
            {
                Id = command.CustomerId,
                Name = "John Doe",
                IsActive = true
            };

            if (customer == null)
            {
                scope.MarkFailed("Customer not found");
                throw new EntityNotFoundException(nameof(Customer), command.CustomerId);
            }

            if (!customer.IsActive)
            {
                scope.MarkFailed("Customer is inactive");
                throw new BusinessRuleViolationException(
                    "Cannot create order for inactive customer",
                    "CUSTOMER_INACTIVE");
            }

            scope.SetTag("customer.name", customer.Name);
            scope.MarkSucceeded();
            return customer;
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Example 3: Calculate order total with telemetry context.
    /// </summary>
    private decimal CalculateOrderTotal(
        CreateOrderCommand command,
        IOperationScope operationScope)
    {
        using var scope = _telemetry.StartOperation("CalculateOrderTotal", "calculation");

        try
        {
            var total = command.Items.Sum(i => i.Quantity * i.UnitPrice);
            scope.SetTag("order.total", total);
            scope.MarkSucceeded();
            return total;
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Example 4: Create order in database with telemetry context.
    /// </summary>
    private async Task<Guid> CreateOrderInDatabase(
        CreateOrderCommand command,
        CustomerDto customer,
        decimal orderTotal,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("CreateOrderInDatabase", "mutation");
        scope.SetTag("database.operation", "INSERT");

        try
        {
            // Simulate database insert
            var orderId = Guid.NewGuid();
            
            // var orderId = await _repository.InsertOrderAsync(new Order
            // {
            //     CustomerId = command.CustomerId,
            //     Total = orderTotal,
            //     Status = "Pending"
            // }, ct);

            scope.SetTag("order.id", orderId);
            scope.MarkSucceeded();
            return orderId;
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Example 5: Emit domain event with telemetry context.
    /// </summary>
    private void EmitOrderCreatedEvent(Guid orderId, IOperationScope operationScope)
    {
        using var scope = _telemetry.StartOperation("EmitOrderCreatedEvent", "event");
        scope.SetTag("event.type", "OrderCreated");
        scope.SetTag("order.id", orderId);

        try
        {
            // Emit domain event
            // await _eventPublisher.PublishAsync(new OrderCreatedDomainEvent(orderId));

            scope.MarkSucceeded();
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }
}

// Supporting types
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemCommand> Items);

public record OrderItemCommand(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public record OrderCreatedResult(Guid? OrderId = null);

public record CustomerDto(Guid Id, string Name, bool IsActive);

public class Customer { }
