# ADR-005: Error Handling Strategy

**Status:** Accepted
**Date:** 2026-01-01

## Context

Inconsistent error handling leads to unpredictable APIs, swallowed exceptions, and poor observability. We need a unified approach that distinguishes expected errors (business rules) from unexpected errors (system failures).

## Decision

### Result<T> for Expected Failures

Application services return `Result<T>` for operations that can fail in expected ways:

```csharp
public async Task<Result<Order>> GetOrderAsync(Guid id, CancellationToken ct)
{
    var order = await _repo.FindAsync(id, ct);
    if (order is null)
        return Result.Failure<Order>(Error.NotFound("Order.NotFound", $"Order {id} not found"));
    return Result.Success(order);
}
```

### Exceptions for Unexpected Failures

Infrastructure failures (network errors, database unavailable) throw exceptions. These propagate to the global exception handler middleware which:
1. Logs the full exception with CorrelationId
2. Returns a sanitized ProblemDetails response (no stack trace)
3. Maps exception types to HTTP status codes

### Exception Hierarchy

```
DomainException (base)
  ├── ValidationException (400)
  ├── NotFoundException (404)
  ├── ConflictException (409)
  └── UnauthorizedException (401)
```

### Forbidden Patterns

- Empty catch blocks
- `catch (Exception) { /* swallow */ }`
- Returning null instead of Result.Failure
- Logging exceptions as strings (use `_logger.LogError(ex, ...)`)

## Consequences

- Expected failures are explicit in method signatures
- Unexpected failures bubble up cleanly to centralized handling
- API consumers get consistent `ProblemDetails` responses
- All errors are observable via structured logging

See: docs/standards/exception-handling.md
