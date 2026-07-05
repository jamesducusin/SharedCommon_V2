# SharedCommon.ApiVersioning

API versioning infrastructure for ASP.NET Core services. Supports URL-segment, header, query-string, and media-type versioning strategies with OpenAPI/Swagger integration.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.ApiVersioning
```

## Registration

```csharp
builder.Services.AddSharedApiVersioning(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "ApiVersioning": {
      "DefaultVersion": "1.0",
      "AssumeDefaultWhenUnspecified": true,
      "ReportApiVersions": true,
      "Strategy": {
        "UrlSegment": true,
        "QueryString": false,
        "Header": false,
        "MediaType": false
      }
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `DefaultVersion` | `"1.0"` | Used when no version is specified and `AssumeDefaultWhenUnspecified` is true. |
| `AssumeDefaultWhenUnspecified` | `true` | Avoids breaking unversioned clients during migration. |
| `ReportApiVersions` | `true` | Adds `api-supported-versions` header to all responses. |
| `Strategy.UrlSegment` | `true` | Reads version from `/api/v1/...`. Recommended. |
| `Strategy.QueryString` | `false` | Reads from `?api-version=1.0`. |
| `Strategy.Header` | `false` | Reads from `X-Api-Version` header. |
| `Strategy.MediaType` | `false` | Reads from `Accept: application/json;v=2.0`. |

---

## Controller Setup

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[ApiVersion("1.0")]
public class OrdersV1Controller : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(new[] { "order-1", "order-2" });
}

[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[ApiVersion("2.0")]
public class OrdersV2Controller : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? cursor)
        => Ok(new { items = new[] { "order-1" }, nextCursor = cursor });
}
```

---

## Deprecating a Version

```csharp
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
public class OrdersController : ControllerBase { }
```

The response will include:
```
api-supported-versions: 2.0
api-deprecated-versions: 1.0
```

---

## What Gets Registered

| Service | Notes |
|---------|-------|
| `ApiVersioningOptions` | Singleton (Options). Validated at startup. |
| Asp.Versioning middleware | Configured per `Strategy` settings. |
| API Explorer groups | Named `"v{version}"` for Swagger group generation. |
