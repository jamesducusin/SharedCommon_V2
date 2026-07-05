# Logging Guidelines

See ADR-002 for the strategic decision.

## Log Levels

| Level | When to use | Example |
|-------|-------------|---------|
| `Trace` | Very detailed, dev only | Loop iterations, raw HTTP bytes |
| `Debug` | Diagnostic info, dev only | Cache key lookup, method arguments |
| `Information` | Normal operations | Request received, order created |
| `Warning` | Recoverable issue | Retry attempt, cache miss |
| `Error` | Operation failed, needs attention | Payment declined, DB timeout |
| `Critical` | Service is down | Cannot connect to DB, out of memory |

## Structured Properties

```csharp
// Good — structured, filterable
_logger.LogInformation("Order {OrderId} created for customer {CustomerId} with {ItemCount} items",
    order.Id, order.CustomerId, order.Items.Count);

// Bad — unstructured, not filterable
_logger.LogInformation($"Order {order.Id} created for customer {order.CustomerId}");
```

## Logging Exceptions

```csharp
// Good — exception is first argument
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);

// Bad — loses stack trace and exception type
_logger.LogError("Failed to process order: " + ex.Message);
_logger.LogError(ex.ToString());
```

## Log Scopes

Use scopes for request-level context:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["UserId"] = userId
}))
{
    // All logs in this scope include CorrelationId and UserId
}
```

## PII Rules

**Never log:**
- Passwords, tokens, API keys
- Full credit card numbers
- Social security numbers
- Full email addresses in production logs (use userId instead)
- Full request/response bodies containing sensitive fields

**Allowed:**
- UserId, OrderId, CustomerId (non-PII identifiers)
- Last 4 digits of card, first 3 chars of token (for debugging)
- Sanitized error descriptions

## Performance Logging

For operations >100ms:
```csharp
var sw = Stopwatch.StartNew();
try
{
    var result = await _repository.GetAsync(id, ct).ConfigureAwait(false);
    _logger.LogInformation("Fetched order {OrderId} in {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
    return result;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to fetch order {OrderId} after {ElapsedMs}ms", id, sw.ElapsedMilliseconds);
    throw;
}
```
