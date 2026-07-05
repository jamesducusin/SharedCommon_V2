# Phase 2: Configuration Best Practices

## appsettings.json - Shared Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Templates": "Information"
    }
  },
  "HealthChecks": {
    "DatabaseTimeout": "00:00:05",
    "CacheTimeout": "00:00:03",
    "MessagingTimeout": "00:00:05",
    "UnhealthyThreshold": 2,
    "DegradedThreshold": 1
  },
  "OpenTelemetry": {
    "ServiceName": "Templates.Api",
    "ServiceVersion": "1.0.0",
    "Enabled": true,
    "SamplingProbability": 0.1,
    "ExporterType": "otlp"
  },
  "Resilience": {
    "HttpClient": {
      "TimeoutSeconds": 30,
      "MaxParallelRequests": 0,
      "BulkheadQueueDepth": 100,
      "CircuitBreakerThreshold": 3,
      "CircuitBreakerWindowSeconds": 30,
      "RetryCount": 3,
      "InitialBackoffMs": 100
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TemplatesDb;Trusted_Connection=true;"
  }
}
```

## appsettings.Development.json - Development Overrides

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Templates": "Debug"
    }
  },
  "HealthChecks": {
    "DatabaseTimeout": "00:00:10",
    "CacheTimeout": "00:00:10"
  },
  "OpenTelemetry": {
    "Enabled": false,
    "SamplingProbability": 1.0
  },
  "Resilience": {
    "HttpClient": {
      "TimeoutSeconds": 60,
      "CircuitBreakerThreshold": 10
    }
  }
}
```

## appsettings.Staging.json - Staging Overrides

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "HealthChecks": {
    "DatabaseTimeout": "00:00:05"
  },
  "OpenTelemetry": {
    "Enabled": true,
    "SamplingProbability": 0.5,
    "ExporterType": "otlp",
    "OtlpExporterEndpoint": "http://otel-collector:4317"
  },
  "Resilience": {
    "HttpClient": {
      "TimeoutSeconds": 30,
      "CircuitBreakerThreshold": 5
    }
  }
}
```

## appsettings.Production.json - Production Overrides

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "HealthChecks": {
    "DatabaseTimeout": "00:00:05",
    "DegradedThreshold": 1
  },
  "OpenTelemetry": {
    "Enabled": true,
    "SamplingProbability": 0.1,
    "ExporterType": "otlp",
    "OtlpExporterEndpoint": "http://otel-collector:4317"
  },
  "Resilience": {
    "HttpClient": {
      "TimeoutSeconds": 30,
      "CircuitBreakerThreshold": 3,
      "CircuitBreakerWindowSeconds": 30
    }
  }
}
```

## Environment Variables - Production Deployment

```bash
# Authentication
export ASPNETCORE_ENVIRONMENT=Production
export JWT_ISSUER=https://auth.company.com
export JWT_AUDIENCE=templates-api
export JWT_SIGNING_KEY=<from-vault>

# Database
export DATABASE_CONNECTION_STRING=Server=sql-prod.database.windows.net;Database=TemplatesDb;...
export DATABASE_USER=<from-vault>
export DATABASE_PASSWORD=<from-vault>

# OpenTelemetry
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
export OTEL_SERVICE_NAME=templates-api
export OTEL_TRACES_SAMPLER=parentbased_traceidratio
export OTEL_TRACES_SAMPLER_ARG=0.1

# Logging
export LOG_LEVEL=Information
export LOG_OUTPUT_PATH=/var/log/templates-api/
```

## ServiceCollectionExtensions.cs - Configuration Integration

```csharp
namespace Templates.Api.Infrastructure.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Templates.Application.Common.Telemetry;
using Templates.Infrastructure.Resilience;
using Templates.Infrastructure.Persistence.Migrations;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register Phase 2 services: health checks, telemetry, resilience, migrations.
    /// </summary>
    public static IServiceCollection AddPhase2Services(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Health checks
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // Telemetry
        services.AddScoped<ITelemetryService, TelemetryService>();

        // Database migrations
        services.AddDatabaseMigrations();

        // Resilience policies on HTTP clients
        services.AddHttpClient<ExternalServiceClient>()
            .AddResiliencePolicies();

        return services;
    }

    /// <summary>
    /// Configure OpenTelemetry for distributed tracing (if enabled in config).
    /// </summary>
    public static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otelConfig = configuration.GetSection("OpenTelemetry");
        
        if (!otelConfig.GetValue<bool>("Enabled"))
            return services;

        var serviceName = otelConfig.GetValue<string>("ServiceName") ?? "Templates.Api";
        var serviceVersion = otelConfig.GetValue<string>("ServiceVersion") ?? "1.0.0";
        var endpoint = otelConfig.GetValue<string>("OtlpExporterEndpoint") ?? "http://localhost:4317";
        var samplingProbability = otelConfig.GetValue<double>("SamplingProbability", 0.1);

        // TODO: Add OpenTelemetry SDK registration
        // Example:
        // services.AddOpenTelemetry()
        //     .WithTracing(builder => builder
        //         .AddAspNetCoreInstrumentation()
        //         .AddHttpClientInstrumentation()
        //         .AddSqlClientInstrumentation()
        //         .AddOtlpExporter(opts => opts.Endpoint = new Uri(endpoint)))

        return services;
    }
}
```

## Health Check Configuration

### Custom Health Check Service

The `HealthCheckService` automatically reads from `IConfiguration`:

```csharp
public class HealthCheckService : IHealthCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        IConfiguration configuration,
        ILogger<HealthCheckService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResponse> CheckHealthAsync()
    {
        var dbTimeout = _configuration.GetValue<TimeSpan>(
            "HealthChecks:DatabaseTimeout", 
            TimeSpan.FromSeconds(5));

        // Use dbTimeout in database check...
    }
}
```

### Configuration Keys

| Key | Type | Default | Purpose |
|-----|------|---------|---------|
| `HealthChecks:DatabaseTimeout` | TimeSpan | 00:00:05 | DB connection timeout |
| `HealthChecks:CacheTimeout` | TimeSpan | 00:00:03 | Cache check timeout |
| `HealthChecks:MessagingTimeout` | TimeSpan | 00:00:05 | Messaging check timeout |
| `HealthChecks:UnhealthyThreshold` | int | 2 | Failures before unhealthy |
| `HealthChecks:DegradedThreshold` | int | 1 | Failures before degraded |

## Resilience Policy Configuration

### Polly Policy Settings

| Setting | Default | Production | Notes |
|---------|---------|-----------|-------|
| TimeoutSeconds | 30 | 30 | Global HTTP timeout |
| MaxParallelRequests | 0 | 16 | 0 = CPU count * 2 |
| BulkheadQueueDepth | 100 | 100 | Queue when at limit |
| CircuitBreakerThreshold | 3 | 3 | Failures to open |
| CircuitBreakerWindowSeconds | 30 | 30 | Sampling window |
| RetryCount | 3 | 3 | Max retry attempts |
| InitialBackoffMs | 100 | 100 | 1st retry delay |

### Configuration Example

```csharp
// In Program.cs
var resilienceConfig = builder.Configuration.GetSection("Resilience:HttpClient");

services.AddHttpClient<ApiClient>()
    .AddResiliencePolicies(resilienceConfig);

// ResiliencePolicy.cs reads configuration:
public static IHttpClientBuilder AddResiliencePolicies(
    this IHttpClientBuilder builder,
    IConfigurationSection? config = null)
{
    var timeoutSeconds = config?.GetValue<int>("TimeoutSeconds") ?? 30;
    var retryCount = config?.GetValue<int>("RetryCount") ?? 3;
    var circuitThreshold = config?.GetValue<int>("CircuitBreakerThreshold") ?? 3;

    return builder
        .AddPolicyHandler(GetHttpTimeoutPolicy(TimeSpan.FromSeconds(timeoutSeconds)))
        .AddPolicyHandler(GetHttpBulkheadPolicy())
        .AddPolicyHandler(GetHttpCircuitBreakerPolicy(circuitThreshold))
        .AddPolicyHandler(GetHttpRetryPolicy(retryCount));
}
```

## Database Migration Configuration

### Migration Storage

Migrations stored as embedded resources in project:

```xml
<!-- Templates.Infrastructure.csproj -->
<ItemGroup>
    <EmbeddedResource Include="Persistence/Migrations/Scripts/*.sql" />
</ItemGroup>
```

### Migration Script Naming

```
Scripts/
├── 001-Initial-Schema.sql              # Tables, indexes
├── 002-Add-Auditing-Columns.sql        # CreatedAt, UpdatedBy, etc.
├── 003-Create-Stored-Procedures.sql    # Performance-critical queries
├── 004-Add-Partitioning.sql            # Table partitions for large tables
└── 999-Seed-Reference-Data.sql         # Reference data, lookup tables
```

### Connection String Per Environment

```csharp
// Program.cs
var connectionString = app.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not configured");

// Or from environment variable
var envConnString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
if (!string.IsNullOrEmpty(envConnString))
    connectionString = envConnString;
```

## Telemetry Configuration

### ActivitySource Setup

```csharp
// In TelemetryService.cs
private static readonly ActivitySource _activitySource = 
    new ActivitySource("Templates.Application", "1.0.0");

private static readonly Meter _meter = 
    new Meter("Templates.Application.Metrics", "1.0.0");
```

### Operation Types

Use consistent operation types for filtering:

- `"command"` - Command/mutation operations
- `"query"` - Query/read operations  
- `"event"` - Domain events
- `"validation"` - Validation operations
- `"calculation"` - Business calculations
- `"mutation"` - Database inserts/updates/deletes
- `"query"` - Database queries
- `"external"` - External API calls

### Trace Context Headers

Standard W3C Trace Context propagation:

```
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
tracestate: rojo=00f067aa0ba902b7,congo=t61rcWkgMzE
```

## Logging Best Practices

### Structured Logging

```csharp
// Good: Structured logging with properties
_logger.LogInformation(
    "Order created successfully: OrderId={OrderId}, CustomerId={CustomerId}, Total={Total}",
    orderId, customerId, total);

// Bad: String concatenation
_logger.LogInformation($"Order created: {orderId}");
```

### Log Levels

- `Critical`: System can't continue (unrecoverable)
- `Error`: Operation failed, needs investigation
- `Warning`: Degraded state, operation may fail
- `Information`: Significant business events
- `Debug`: Developer troubleshooting (dev only)
- `Trace`: Low-level diagnostic info (disabled by default)

### Correlation IDs

Automatically included via HTTP TraceIdentifier:

```csharp
// In middleware
_logger.LogInformation(
    "Request started: TraceId={TraceId}, Path={Path}",
    context.TraceIdentifier, context.Request.Path);
```

## Performance Tuning

### Database

```csharp
// Timeout for health checks (shorter than general timeout)
var dbTimeout = TimeSpan.FromSeconds(5);

// Connection pool sizing
var connectionString = "...;Min Pool Size=10;Max Pool Size=30;";
```

### Resilience

```csharp
// Dev: More lenient (faster iteration)
CircuitBreakerThreshold: 10
TimeoutSeconds: 60

// Prod: More aggressive (faster failure detection)
CircuitBreakerThreshold: 3
TimeoutSeconds: 30
```

### Tracing

```csharp
// Dev: Sample everything
SamplingProbability: 1.0

// Prod: Sample 10% (reduce overhead)
SamplingProbability: 0.1

// High-volume environments: 1-5%
SamplingProbability: 0.01
```

## Security Considerations

### Sensitive Configuration

Never commit to source control:

```bash
# Use Azure Key Vault, AWS Secrets Manager, or similar
dotnet user-secrets set "Jwt:SigningKey" "<secret>" --project src/Templates.Api
```

### Health Endpoint Security

```csharp
// Require authentication for detailed health
app.MapGet("/health/detailed",
    (IHealthCheckService service) => service.CheckHealthAsync())
    .RequireAuthorization("HealthCheckPolicy");

// Allow public liveness/readiness
app.MapGet("/health/live", 
    (IHealthCheckService service) => service.IsLiveAsync())
    .AllowAnonymous();
```

### Database Credentials

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-db;Database=TemplatesDb;Integrated Security=false;User Id=app_user;Password=#{DATABASE_PASSWORD}#"
  }
}
```

Use parameter substitution in deployment pipeline.

## Troubleshooting Configuration Issues

### Health Check Configuration Not Applied

1. Verify configuration keys match exactly (case-sensitive)
2. Check `appsettings.Environment.json` is loaded
3. Use configuration builder debugger:

```csharp
var config = builder.Configuration;
var dbTimeout = config["HealthChecks:DatabaseTimeout"];
_logger.LogInformation("Database timeout: {DbTimeout}", dbTimeout);
```

### Resilience Policies Not Activating

1. Verify `AddResiliencePolicies()` is called on HttpClientBuilder
2. Check policies are added in correct order
3. Enable policy logging:

```csharp
.AddTransientHttpErrorPolicy(p => 
    p.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            _logger.LogWarning("Circuit breaker opened for {Duration}", duration);
            return Task.CompletedTask;
        }))
```

### OpenTelemetry Not Exporting

1. Check `Enabled: true` in configuration
2. Verify OTLP exporter endpoint is reachable
3. Confirm environment variable takes precedence:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317
```

4. Enable diagnostic logging:

```csharp
AppContext.SetSwitch("System.Diagnostics.ActivityListener.ThrowOnUnknownActivityId", true);
```

## References

- [.NET Configuration Provider Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Environment-Based appsettings](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#appsettingsjson)
- [User Secrets Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [OpenTelemetry Configuration](https://opentelemetry.io/docs/instrumentation/net/getting-started/#initialize-the-global-sdk)
