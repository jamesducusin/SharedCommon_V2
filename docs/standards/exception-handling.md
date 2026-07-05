# Exception Handling Standards

See ADR-005 for the strategic decision.

## When to Throw vs Return Result

| Situation | Use |
|-----------|-----|
| Business rule violation (expected) | `Result.Failure(Error.Validation(...))` |
| Resource not found (expected) | `Result.Failure(Error.NotFound(...))` |
| Concurrency conflict (expected) | `Result.Failure(Error.Conflict(...))` |
| Null/invalid argument (programming error) | `ArgumentNullException`, `ArgumentException` |
| Infrastructure failure (unexpected) | Let it propagate (exception) |
| Cannot recover, service must stop | `Critical` log + let propagate |

## Result<T> Pattern

```csharp
public async Task<Result<Order>> GetOrderAsync(Guid id, CancellationToken ct)
{
    if (id == Guid.Empty)
        return Result.Failure<Order>(Error.Validation("Order.InvalidId", "Order ID cannot be empty"));

    var order = await _repo.FindAsync(id, ct).ConfigureAwait(false);
    if (order is null)
        return Result.Failure<Order>(Error.NotFound("Order.NotFound", $"Order {id} not found"));

    return Result.Success(order);
}
```

## Controller Usage

```csharp
var result = await _orderService.GetOrderAsync(id, ct);

return result.IsSuccess
    ? Ok(result.Value)
    : result.Error.Code switch
    {
        "Order.NotFound" => NotFound(result.Error.Description),
        "Order.InvalidId" => BadRequest(result.Error.Description),
        _ => StatusCode(500)
    };
```

Or use `_responseBuilder.From(result)` for automatic mapping.

## Global Exception Handler

Registered by `SharedCommon.Middlewares`:

```csharp
app.UseExceptionHandler(builder => builder.Run(async context => {
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);

    context.Response.StatusCode = exception is DomainException de
        ? de.StatusCode
        : 500;

    await context.Response.WriteAsJsonAsync(new ProblemDetails {
        Title = "An error occurred",
        Status = context.Response.StatusCode
        // No stack trace — never expose to client
    });
}));
```

## Forbidden Patterns

```csharp
// Forbidden: swallowing
try { ... } catch { }

// Forbidden: lossy logging
catch (Exception ex) { _logger.LogError(ex.Message); throw; }

// Correct
catch (Exception ex) { _logger.LogError(ex, "Failed to process {OrderId}", id); throw; }

// Forbidden: catching all and returning null
catch (Exception) { return null; }
```
