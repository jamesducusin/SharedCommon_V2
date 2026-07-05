namespace Templates.Application.Features.Orders.GetById;

using Templates.Application.Common.Telemetry;
using Templates.Domain.Common.Exceptions;

/// <summary>
/// Example query handler showing how to use telemetry for read operations.
/// Demonstrates caching patterns, query optimization, and error handling.
/// </summary>
public class GetOrderByIdQueryHandlerWithTelemetry
{
    private readonly ITelemetryService _telemetry;
    // private readonly IOrderRepository _repository;
    // private readonly IDistributedCache _cache;

    public GetOrderByIdQueryHandlerWithTelemetry(
        ITelemetryService telemetry)
        // IOrderRepository repository,
        // IDistributedCache cache)
    {
        _telemetry = telemetry;
        // _repository = repository;
        // _cache = cache;
    }

    /// <summary>
    /// Handle query to retrieve order by ID with caching and telemetry.
    /// Demonstrates: cache hit/miss tracking, query optimization, and nested operations.
    /// </summary>
    public async Task<OrderDto?> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken ct)
    {
        // Start operation span for read query
        using var operationScope = _telemetry.StartOperation("GetOrderById", "query");
        operationScope.SetTag("order.id", query.OrderId);

        try
        {
            // Attempt 1: Cache lookup
            var cachedOrder = await GetFromCacheAsync(query.OrderId, operationScope, ct);
            if (cachedOrder != null)
            {
                operationScope.SetTag("cache.hit", true);
                _telemetry.RecordMetric("cache.hit", 1, new() { { "operation", "GetOrderById" } });
                operationScope.MarkSucceeded();
                return cachedOrder;
            }

            // Cache miss - record metric
            operationScope.SetTag("cache.hit", false);
            _telemetry.RecordMetric("cache.miss", 1, new() { { "operation", "GetOrderById" } });

            // Attempt 2: Database query
            var order = await FetchFromDatabaseAsync(query.OrderId, operationScope, ct);
            if (order == null)
            {
                operationScope.MarkFailed("Order not found");
                throw new EntityNotFoundException(nameof(Order), query.OrderId);
            }

            // Cache the result for next time
            await SetCacheAsync(order, operationScope, ct);

            operationScope.MarkSucceeded();
            _telemetry.RecordMetric("orders.fetched", 1, new() { { "source", "database" } });

            return order;
        }
        catch (Exception ex)
        {
            operationScope.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Example 1: Cache lookup with telemetry.
    /// </summary>
    private async Task<OrderDto?> GetFromCacheAsync(
        Guid orderId,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("GetFromCache", "query");
        scope.SetTag("cache.key", $"order:{orderId}");

        try
        {
            // Simulate cache lookup
            // var cachedJson = await _cache.GetStringAsync($"order:{orderId}", ct);
            // if (cachedJson == null) return null;
            // var cached = JsonSerializer.Deserialize<OrderDto>(cachedJson);

            // For demo - always miss
            await Task.Delay(1, ct);
            scope.SetTag("cache.result", "miss");
            return null;
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("cache.error", 1);
            throw;
        }
    }

    /// <summary>
    /// Example 2: Database query with optimization tagging.
    /// </summary>
    private async Task<OrderDto?> FetchFromDatabaseAsync(
        Guid orderId,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("FetchFromDatabase", "query");
        scope.SetTag("database.query", "GetOrderById");
        scope.SetTag("database.timeout", "30s");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Simulate database query
            var order = new OrderDto
            {
                Id = orderId,
                OrderNumber = "ORD-001",
                CustomerId = Guid.NewGuid(),
                Total = 150.00m,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Items = new List<OrderItemDto>
                {
                    new(Guid.NewGuid(), "Item 1", 2, 50.00m),
                    new(Guid.NewGuid(), "Item 2", 1, 50.00m)
                }
            };

            // Simulate query execution
            // var order = await _repository.GetOrderWithDetailsAsync(orderId, ct);

            sw.Stop();

            scope.SetTag("database.duration_ms", sw.ElapsedMilliseconds);
            scope.SetTag("items.count", order?.Items.Count ?? 0);
            scope.SetTag("database.result", order != null ? "found" : "not_found");

            // Record metric for query latency
            _telemetry.RecordMetric("database.query.duration_ms", 
                (double)sw.ElapsedMilliseconds,
                new() { { "query", "GetOrderById" } });

            scope.MarkSucceeded();
            return order;
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("database.error", 1, new() { { "operation", "GetOrderById" } });
            throw;
        }
    }

    /// <summary>
    /// Example 3: Cache storage with telemetry.
    /// </summary>
    private async Task SetCacheAsync(
        OrderDto order,
        IOperationScope operationScope,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("SetCache", "mutation");
        scope.SetTag("cache.key", $"order:{order.Id}");
        scope.SetTag("cache.ttl_minutes", 60);

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Simulate cache set
            // var json = JsonSerializer.Serialize(order);
            // await _cache.SetStringAsync(
            //     $"order:{order.Id}",
            //     json,
            //     new DistributedCacheEntryOptions
            //     {
            //         AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
            //     },
            //     ct);

            await Task.Delay(1, ct);

            sw.Stop();

            scope.SetTag("cache.size_bytes", 256); // Estimate
            scope.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);
            scope.MarkSucceeded();

            _telemetry.RecordMetric("cache.set", 1, new() { { "operation", "GetOrderById" } });
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("cache.set.error", 1);
            // Cache failure should not fail the operation
            return;
        }
    }
}

// Supporting types
public record GetOrderByIdQuery(Guid OrderId);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    decimal Total,
    string Status,
    DateTime CreatedAt,
    List<OrderItemDto> Items);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public class Order { }
