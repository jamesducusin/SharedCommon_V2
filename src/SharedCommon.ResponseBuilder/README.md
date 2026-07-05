# SharedCommon.ResponseBuilder

Standardized HTTP response envelope for ASP.NET Core APIs. Every success response is wrapped in `ApiResponse<T>`. Every error response is RFC 9457 `ProblemDetails`. Correlation IDs are injected automatically.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.ResponseBuilder
```

## Registration

```csharp
builder.Services.AddSharedResponseBuilder();
```

No configuration section — it reads the correlation ID from `IRequestContext` (registered by `SharedCommon.Core`).

## Response Envelope

### Success

```json
{
  "success": true,
  "data": { "id": 1, "name": "Widget" },
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Paged list

```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 134,
    "totalPages": 7,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Error (ProblemDetails)

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Order 42 was not found.",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## Usage

### IResponseBuilder — recommended

Inject `IResponseBuilder` into controllers. It auto-injects the correlation ID and maps `Result<T>` to the correct HTTP status:

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController(IResponseBuilder response, IOrderService orders) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var result = await orders.GetByIdAsync(id, ct);
        return response.FromResult(result);
        // Result<Order>.Success  → 200 { success: true, data: {...} }
        // Result<Order>.Failure "NOT_FOUND" → 404 ProblemDetails
        // Result<Order>.Validation → 422 ValidationProblemDetails
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page, CancellationToken ct)
    {
        var (items, total) = await orders.ListAsync(page, ct);
        var pagination = new PaginationInfo(page, pageSize: 20, totalCount: total);
        return response.Paged(items, pagination);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand cmd, CancellationToken ct)
    {
        var result = await orders.CreateAsync(cmd, ct);
        return result is Result<Order>.Success s
            ? response.Created("GetOrder", new { id = s.Data.Id }, s.Data)
            : response.FromResult(result);
    }
}
```

### Failure code → HTTP status mapping

| Result.Failure Code | HTTP Status |
|--------------------|-------------|
| `NOT_FOUND` | 404 |
| `UNAUTHORIZED` | 401 |
| `FORBIDDEN` | 403 |
| `CONFLICT` | 409 |
| `RATE_LIMITED` | 429 |
| _(anything else)_ | 500 |

### Extension methods on Result&lt;T&gt;

```csharp
// In a minimal API or without IResponseBuilder:
var result = await service.GetAsync(id, ct);

// Convert to IActionResult
IActionResult actionResult = result.ToActionResult();

// Convert to ApiResponse<T> (null on non-success)
ApiResponse<Order>? envelope = result.ToApiResponse();
```

### ProblemDetailsFactory — direct use

For cases where you need to build error responses manually:

```csharp
return BadRequest(ProblemDetailsFactory.Validation(new Dictionary<string, string[]>
{
    ["email"] = ["Email is required."]
}, correlationId: requestContext.CorrelationId.Value));

return NotFound(ProblemDetailsFactory.NotFound("Order 42 not found."));
return StatusCode(429, ProblemDetailsFactory.TooManyRequests());
```

> **Disambiguation**: use the fully qualified `SharedCommon.ResponseBuilder.ProblemDetailsFactory` to avoid conflict with `Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory`.

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IResponseBuilder` | Scoped | Reads `IRequestContext` for the correlation ID. |
