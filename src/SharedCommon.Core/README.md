# SharedCommon.Core

Foundation package. Zero external dependencies. Required by every other SharedCommon package.

Provides: `Result<T>` discriminated union, `IRequestContext` for correlation ID propagation, and `CoreOptions` for shared platform identity.

## Installation

```bash
dotnet add package SharedCommon.Core
```

## Registration

```csharp
builder.Services.AddSharedCommonCore(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Core": {
      "ApplicationName": "OrderService",
      "EnvironmentName": "Production",
      "Version": "1.0.0",
      "AllowedOrigins": ["https://app.example.com"]
    }
  }
}
```

| Property | Required | Default | Notes |
|----------|----------|---------|-------|
| `ApplicationName` | Yes | — | Max 50 chars. Appears in logs and traces. |
| `EnvironmentName` | Yes | — | Must be `Development`, `Staging`, or `Production`. |
| `Version` | Yes | — | Semantic version string, e.g. `1.2.3`. |
| `AllowedOrigins` | No | `[]` | CORS origins forwarded to downstream packages. |

## The Result Pattern

All SharedCommon services return `Result<T>` or `Result` instead of throwing exceptions for expected failures. There are three cases:

```csharp
// Result<T> — typed success payload
public static Result<T> Ok(T data)       // success
public static Result<T> Fail(...)        // failure with code + message
public static Result<T> Invalid(...)     // validation failure with field errors

// Result — untyped (for void operations)
new Result.Success()
new Result.Failure("NOT_FOUND", "Order not found")
new Result.Validation(new Dictionary<string, string[]>
{
    ["email"] = ["Email is required.", "Email must be valid."]
})
```

Pattern-match the result at your API boundary:

```csharp
var result = await _orderService.GetByIdAsync(id, ct);

return result switch
{
    Result<Order>.Success s   => Ok(s.Data),
    Result<Order>.Validation v => UnprocessableEntity(v.Errors),
    Result<Order>.Failure f   => f.Code switch
    {
        "NOT_FOUND"  => NotFound(f.Message),
        "FORBIDDEN"  => Forbid(),
        _            => Problem(f.Message)
    },
    _ => StatusCode(500)
};
```

Or use `IResponseBuilder` (from `SharedCommon.ResponseBuilder`) to map automatically.

## IRequestContext

`IRequestContext` is injected per-request and carries the correlation ID. It is automatically populated by `SharedCommon.Middlewares`.

```csharp
public class OrderService(IRequestContext requestContext)
{
    public async Task<Result<Order>> CreateAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var correlationId = requestContext.CorrelationId.Value;
        // correlationId is guaranteed non-null when middleware is wired
        ...
    }
}
```

## Guard Clauses

`Guard` provides argument validation that fails fast with clear messages. Uses `[CallerArgumentExpression]` so the parameter name appears in the exception automatically:

```csharp
public class OrderService(IRepository repo)
{
    public async Task<Result<Order>> GetAsync(Guid id, CancellationToken ct)
    {
        Guard.AgainstEmptyGuid(id);                      // ArgumentException if Guid.Empty
        Guard.AgainstNull(repo);                         // ArgumentNullException if null
        Guard.AgainstOutOfRange(pageSize, 1, 200);       // ArgumentOutOfRangeException if outside range
        Guard.AgainstNullOrWhiteSpace(customerName);     // ArgumentException if blank
        ...
    }
}
```

| Method | Throws | When |
|--------|--------|------|
| `AgainstNull<T>` | `ArgumentNullException` | value is null |
| `AgainstNullOrEmpty` | `ArgumentException` | string is null or `""` |
| `AgainstNullOrWhiteSpace` | `ArgumentException` | string is null, `""`, or whitespace |
| `AgainstEmpty<T>` | `ArgumentException` | collection is null or empty |
| `AgainstLessThan` | `ArgumentOutOfRangeException` | value < min |
| `AgainstGreaterThan` | `ArgumentOutOfRangeException` | value > max |
| `AgainstOutOfRange` | `ArgumentOutOfRangeException` | value outside [min, max] |
| `AgainstEmptyGuid` | `ArgumentException` | `Guid.Empty` |
| `AgainstInvalidState` | `ArgumentException` | condition is true |
| `AgainstExceedingLength` | `ArgumentException` | string exceeds max chars |

---

## Paged Results

`PagedResult<T>` and `Pagination` are the platform's standard types for list operations:

```csharp
// Build pagination from query params (clamped automatically)
var pagination = Pagination.Of(page: request.Page, pageSize: request.PageSize);

// Apply in a repository
var items = await _db.Orders
    .Skip(pagination.Offset)
    .Take(pagination.PageSize)
    .ToListAsync(ct);

return new PagedResult<Order>(items, totalCount: totalCount, pagination);
```

```csharp
// Alternatively, slice an in-memory list
var result = PagedResult<Order>.From(allOrders, pagination);

// Navigation metadata is computed automatically
bool hasMore = result.HasNextPage;
int pages    = result.TotalPages;
```

---

## Domain Interfaces

Use these in your domain layer to signal entity identity and DDD roles:

```csharp
// Entity — identity by Id
public class Order : IEntity<Guid>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
}

// Aggregate root — repository boundary + domain events
public class Customer : IAggregateRoot<Guid>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    private readonly List<object> _events = [];
    public IReadOnlyList<object> DomainEvents => _events;
}

// Value object — implement as a record for structural equality
public record Money(decimal Amount, string Currency) : IValueObject;
```

---

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IRequestContext` | Scoped | One instance per HTTP request. |
| `CoreOptions` | Options | Validated at startup. |
