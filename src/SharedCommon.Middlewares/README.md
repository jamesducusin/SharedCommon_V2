# SharedCommon.Middlewares

ASP.NET Core pipeline middleware for cross-cutting concerns: exception handling, correlation ID propagation, and structured request logging.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Middlewares
```

## Registration

```csharp
// Services
builder.Services.AddSharedCommonMiddlewares(builder.Configuration);

// Pipeline — order matters
app.UseSharedCommonExceptionHandling();   // must be first (outermost)
app.UseSharedCommonCorrelationId();
app.UseSharedCommonRequestLogging();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

## Configuration

```json
{
  "SharedCommon": {
    "Middlewares": {
      "ExceptionHandling": {
        "Enabled": true,
        "IncludeStackTrace": false,
        "LogExceptions": true
      },
      "CorrelationId": {
        "Enabled": true,
        "HeaderName": "X-Correlation-ID",
        "GenerateIfMissing": true
      },
      "RequestLogging": {
        "Enabled": true,
        "LogRequestBody": false,
        "LogResponseBody": false,
        "ExcludePaths": ["/health", "/metrics"],
        "MaxBodySizeToLog": 1024
      }
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `ExceptionHandling.IncludeStackTrace` | `false` | Set `true` in Development only. Never in Production. |
| `CorrelationId.HeaderName` | `X-Correlation-ID` | Read from incoming request; written to response. |
| `CorrelationId.GenerateIfMissing` | `true` | New UUIDs generated when header is absent. |
| `RequestLogging.ExcludePaths` | `["/health", "/metrics"]` | Exact prefix match. |
| `RequestLogging.LogRequestBody` | `false` | Enable only in Development — risk of logging PII. |

## What Each Middleware Does

### UseSharedCommonExceptionHandling

Catches all unhandled exceptions and returns a JSON `ProblemDetails` response (RFC 9457). The caller never sees a raw exception or stack trace.

```
→ 500 Internal Server Error
{
  "type": "https://httpstatuses.com/500",
  "title": "Internal Server Error",
  "status": 500,
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

Set `IncludeStackTrace: true` in Development to include the exception detail in the response.

### UseSharedCommonCorrelationId

- Reads `X-Correlation-ID` from the incoming request header
- Generates a new UUID if the header is absent and `GenerateIfMissing` is `true`
- Stores it in `IRequestContext.CorrelationId` for use in services
- Writes it back on the response header

Access the correlation ID anywhere in your request pipeline:

```csharp
public class OrderService(IRequestContext ctx)
{
    public Task DoWorkAsync()
    {
        var id = ctx.CorrelationId.Value;
        // pass id to downstream calls, store in audit log, etc.
    }
}
```

### UseSharedCommonRequestLogging

Logs a structured entry for every non-excluded request:

```json
{
  "Method": "POST",
  "Path": "/api/orders",
  "StatusCode": 201,
  "ElapsedMs": 42,
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `MiddlewareOptions` | Options | Validated at startup. |
| `ExceptionHandlingMiddleware` | — | Wired via `UseSharedCommonExceptionHandling()`. |
| `CorrelationIdMiddleware` | — | Wired via `UseSharedCommonCorrelationId()`. |
| `RequestLoggingMiddleware` | — | Wired via `UseSharedCommonRequestLogging()`. |
