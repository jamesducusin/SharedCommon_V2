# SharedCommon.Auth

JWT authentication and current-user context for ASP.NET Core APIs.

Provides: JWT Bearer scheme registration, `IAuthService` for token issuance and validation, and `ICurrentUser` for accessing the authenticated caller in any service.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Auth
```

## Registration

```csharp
builder.Services.AddSharedCommonAuth(builder.Configuration);

// In the middleware pipeline (after UseRouting):
app.UseAuthentication();
app.UseAuthorization();
```

## Configuration

```json
{
  "SharedCommon": {
    "Auth": {
      "Jwt": {
        "Issuer": "https://auth.example.com",
        "Audience": "https://api.example.com",
        "ExpirationMinutes": 60,
        "RefreshTokenExpirationDays": 7,
        "Algorithm": "HS256",
        "Validation": {
          "ValidateAudience": true,
          "ValidateIssuer": true,
          "ValidateLifetime": true,
          "ClockSkewSeconds": 0
        }
      }
    }
  }
}
```

> **SecretKey** is required but must never appear in `appsettings.json`. Set it via User Secrets or your secrets manager:
> ```bash
> dotnet user-secrets set "SharedCommon:Auth:Jwt:SecretKey" "your-32-char-minimum-secret-key"
> ```

| Property | Required | Default | Notes |
|----------|----------|---------|-------|
| `Jwt.SecretKey` | Yes | — | Min 32 chars. Secrets only, never in config files. |
| `Jwt.Issuer` | Yes | — | Token issuer URI. |
| `Jwt.Audience` | Yes | — | Token audience URI. |
| `Jwt.ExpirationMinutes` | No | `60` | Range: 1–10080 (1 week). |
| `Jwt.RefreshTokenExpirationDays` | No | `7` | Range: 1–365. |
| `Jwt.Algorithm` | No | `HS256` | `HS256` \| `RS256` |
| `Jwt.Validation.ClockSkewSeconds` | No | `0` | Allowable token clock drift. |

## Usage

### Protecting endpoints

Use the standard `[Authorize]` attribute — no extra setup needed:

```csharp
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public IActionResult GetMyOrders()
    {
        var userId = currentUser.UserId;      // Guid
        var email  = currentUser.Email;       // string
        var roles  = currentUser.Roles;       // IReadOnlyList<string>
        return Ok(...);
    }
}
```

### ICurrentUser

`ICurrentUser` is scoped to the request and populated from the JWT claims automatically.

```csharp
public class AuditService(ICurrentUser currentUser)
{
    public AuditEntry CreateEntry(string action) => new()
    {
        UserId    = currentUser.UserId,
        UserEmail = currentUser.Email,
        Action    = action,
        At        = DateTimeOffset.UtcNow,
        IsSystem  = !currentUser.IsAuthenticated
    };
}
```

### IAuthService — issuing tokens

```csharp
public class LoginHandler(IAuthService authService)
{
    public async Task<TokenResponse> HandleAsync(LoginCommand cmd, CancellationToken ct)
    {
        // Validate credentials ...

        var token = await authService.GenerateTokenAsync(new TokenRequest
        {
            UserId = user.Id,
            Email  = user.Email,
            Roles  = user.Roles
        }, ct);

        return new TokenResponse(token.AccessToken, token.RefreshToken);
    }
}
```

### IAuthService — validating tokens

```csharp
var principal = await authService.ValidateTokenAsync(rawToken, ct);
if (principal is null)
    return Unauthorized();
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IAuthService` | Singleton | JWT token generation and validation. |
| `ICurrentUser` | Scoped | Claims from the current HTTP request's JWT. |
| JWT Bearer scheme | — | Registered in `AddAuthentication`. |
