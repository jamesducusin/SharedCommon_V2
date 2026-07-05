# SharedCommon.HealthChecks

Liveness and readiness health check endpoints following Kubernetes probe semantics.

- `/health/live` — fast liveness probe. No external calls. Returns 200 if the process is running.
- `/health/ready` — readiness probe. Checks all critical dependencies (Redis, external HTTP). Returns 200 only when the service can handle traffic.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.HealthChecks
```

## Registration

```csharp
// Services
builder.Services.AddSharedHealthChecks(builder.Configuration);

// Endpoints
app.UseSharedHealthEndpoints();
```

## Configuration

```json
{
  "SharedCommon": {
    "HealthChecks": {
      "DefaultTimeout": "00:00:05",
      "Redis": {
        "Enabled": true,
        "Name": "redis"
      },
      "ExternalHttp": [
        {
          "Name": "payment-api",
          "Uri": "https://payment.internal/health"
        },
        {
          "Name": "inventory-api",
          "Uri": "https://inventory.internal/health"
        }
      ]
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `DefaultTimeout` | `00:00:05` | Max time for any single check. |
| `Redis.Enabled` | `false` | Requires `SharedCommon.Caching` to also be registered. |
| `Redis.Name` | `"redis"` | Name shown in the health report. |
| `ExternalHttp` | `[]` | List of HTTP endpoints to probe. Each must return 2xx. |

## Usage

### Adding custom health checks

Register any `IHealthCheck` implementation via the standard ASP.NET Core API:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<MessageBrokerHealthCheck>("rabbitmq", tags: ["ready"]);
```

Tag your checks with:
- `"live"` — included in `/health/live`
- `"ready"` — included in `/health/ready`

SharedCommon built-in checks are already tagged appropriately (Redis → `"ready"`, external HTTP → `"ready"`).

### Custom health check example

```csharp
public class DatabaseHealthCheck(IDbConnection db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            await db.ExecuteAsync("SELECT 1", ct);
            return HealthCheckResult.Healthy("Database reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
```

### Response format

Both endpoints return JSON via `IHealthCheckReporter`. Implement this interface to customize the output:

```csharp
public class MyHealthReporter : IHealthCheckReporter
{
    public async Task WriteReportAsync(
        HttpContext context,
        HealthReport report,
        CancellationToken ct = default)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;

        var body = JsonSerializer.Serialize(new
        {
            status  = report.Status.ToString(),
            checks  = report.Entries.Select(e => new
            {
                name     = e.Key,
                status   = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        });

        await context.Response.WriteAsync(body, ct);
    }
}

// Register your reporter before AddSharedHealthChecks:
builder.Services.AddSingleton<IHealthCheckReporter, MyHealthReporter>();
builder.Services.AddSharedHealthChecks(builder.Configuration);
```

### Kubernetes probe configuration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15
  failureThreshold: 3
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IHealthCheckReporter` | Singleton | Default JSON reporter. Replace by registering your own before calling `AddSharedHealthChecks`. |
| Redis health check | — | Only when `Redis.Enabled` is `true`. Tagged `"ready"`. |
| External HTTP checks | — | One per entry in `ExternalHttp`. Tagged `"ready"`. |
