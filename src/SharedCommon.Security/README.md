# SharedCommon.Security

HTTP security defaults for ASP.NET Core APIs: security headers, rate limiting, CORS, input validation, and HTTPS enforcement — all configured via `appsettings.json`.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Security
```

## Registration

```csharp
builder.Services.AddSharedSecurity(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Security": {
      "SecurityHeaders": {
        "Enabled": true,
        "StrictTransportSecurity": {
          "Enabled": true,
          "MaxAge": 31536000,
          "IncludeSubdomains": true
        },
        "ContentSecurityPolicy": {
          "Enabled": true,
          "DefaultSrc": "'self'",
          "ScriptSrc": "'self'",
          "StyleSrc": "'self' 'unsafe-inline'"
        },
        "XFrameOptions": { "Policy": "Deny" },
        "ReferrerPolicy": { "Policy": "strict-origin" }
      },
      "RateLimit": {
        "Enabled": true,
        "Backend": "Memory",
        "Policies": {
          "Default":       { "MaxRequests": 100,   "WindowSeconds": 60 },
          "Authenticated": { "MaxRequests": 1000,  "WindowSeconds": 60 },
          "ApiEndpoint":   { "MaxRequests": 10000, "WindowSeconds": 3600 }
        }
      },
      "Cors": {
        "Enabled": true,
        "AllowedOrigins": ["https://app.example.com"],
        "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
        "AllowedHeaders": ["*"],
        "AllowCredentials": false
      },
      "InputValidation": {
        "Enabled": true,
        "MaxUrlLength": 2048,
        "MaxQueryStringLength": 8192,
        "MaxBodySizeBytes": 10485760,
        "BlockSuspiciousPatterns": true
      },
      "Https": {
        "Enforced": true,
        "RedirectStatusCode": 307
      }
    }
  }
}
```

### Key properties

| Property | Default | Notes |
|----------|---------|-------|
| `SecurityHeaders.Enabled` | `true` | All security headers applied by default. |
| `RateLimit.Enabled` | `true` | Sliding window, in-memory by default. |
| `RateLimit.Backend` | `Memory` | `Memory` \| `Redis` |
| `Cors.AllowedOrigins` | `[]` | Required when CORS is enabled. |
| `InputValidation.BlockSuspiciousPatterns` | `true` | Blocks SQLi, XSS, path traversal patterns. |
| `Https.Enforced` | `true` | HTTP → HTTPS redirect. Disable behind a load balancer that terminates TLS. |

## Usage

Security headers are applied automatically via middleware registered by `AddSharedSecurity`. No additional `app.Use*` calls are needed for headers.

For CORS, the policy named `"SharedCommon"` is registered. Use it on specific controllers:

```csharp
[ApiController]
[EnableCors("SharedCommon")]
public class PublicController : ControllerBase { ... }
```

Or apply globally in the pipeline:

```csharp
app.UseCors("SharedCommon");
```

### Rate limiting

The named policies (`Default`, `Authenticated`, `ApiEndpoint`) are registered automatically. Apply them to controllers or endpoints:

```csharp
[EnableRateLimiting("Authenticated")]
public class OrdersController : ControllerBase { ... }
```

Or on a minimal API endpoint:

```csharp
app.MapPost("/api/orders", handler).RequireRateLimiting("Default");
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| CORS policy `"SharedCommon"` | — | Apply with `[EnableCors]` or `app.UseCors`. |
| Rate limit policies | — | Named policies registered in `RateLimitOptions.Policies`. |
| `IRateLimitService` | Singleton | For programmatic rate limit checks. |
| `IInputValidator` | Singleton | For manual input inspection. |
