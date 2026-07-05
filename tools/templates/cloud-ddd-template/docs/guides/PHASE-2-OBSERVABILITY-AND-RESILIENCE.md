# Phase 2: Observability & Resilience Implementation Guide

## Overview

Phase 2 implements critical observability and resilience patterns required for production cloud deployments:

- **Health Checks**: Kubernetes-compatible liveness and readiness probes
- **Distributed Tracing**: OpenTelemetry ActivitySource integration with Serilog
- **Resilience Policies**: Polly patterns for HTTP clients (retry, circuit breaker, timeout, bulkhead)
- **Database Migrations**: DbUp-based versioning with rollback capability
- **Metrics**: Telemetry for business operations and infrastructure

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    HTTP Request                          │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────▼───────────┐
         │ Middleware Stack      │
         │ (Exception, Auth)     │
         └───────────┬───────────┘
                     │
         ┌───────────▼──────────────────┐
         │ Endpoint Handler             │
         │ ┌────────────────────────┐   │
         │ │ TelemetryService       │   │
         │ │ (Activity tracing)     │   │
         │ └────┬────────────────────┘   │
         │      │                        │
         │ ┌────▼────────────────────┐   │
         │ │ Business Logic Layer    │   │
         │ │ ┌──────────────────────┐│   │
         │ │ │ Repository/Service   ││   │
         │ │ └──────────┬───────────┘│   │
         │ │            │            │   │
         │ │    ┌───────▼──────┐    │   │
         │ │    │ Polly Policy │    │   │
         │ │    │ Stack        │    │   │
         │ │    └───────┬──────┘    │   │
         │ │            │           │   │
         │ │    ┌───────▼──────┐    │   │
         │ │    │ External API │    │   │
         │ │    │ / Database   │    │   │
         │ │    └──────────────┘    │   │
         │ └────────────────────────┘   │
         └──────────────────────────────┘
                     │
         ┌───────────▼──────────────┐
         │ Response (with TraceId)  │
         │ to Client                │
         └──────────────────────────┘

Health Checks:
  /health/live    → Liveness (always true, fast)
  /health/ready   → Readiness (checks database)
  /health/detailed → Full status with metrics
```

## Component Reference

### 1. Health Checks (`HealthCheckService.cs`)

**Purpose**: Kubernetes probes and monitoring endpoints

**Key Components**:
- `IHealthCheckService` interface: Public contract for health operations
- `HealthCheckService` implementation: Checks database, cache, messaging dependencies
- `HealthCheckResponse` model: Structured response with status and timings

**Usage**:

```csharp
public class HealthCheckEndpoint
{
    public async Task<IResult> GetLivenessAsync(
        IHealthCheckService healthCheckService)
    {
        var isLive = await healthCheckService.IsLiveAsync();
        return isLive ? Results.Ok() : Results.StatusCode(503);
    }

    public async Task<IResult> GetReadinessAsync(
        IHealthCheckService healthCheckService)
    {
        var isReady = await healthCheckService.IsReadyAsync();
        return isReady ? Results.Ok() : Results.StatusCode(503);
    }

    public async Task<IResult> GetDetailedStatusAsync(
        IHealthCheckService healthCheckService)
    {
        var health = await healthCheckService.CheckHealthAsync();
        return Results.Ok(health);
    }
}
```

**Configuration** (appsettings.json):

```json
{
  "HealthChecks": {
    "DatabaseTimeout": "00:00:05",
    "CacheTimeout": "00:00:03",
    "MessagingTimeout": "00:00:05",
    "UnhealthyThreshold": 2,
    "DegradedThreshold": 1
  }
}
```

### 2. Distributed Tracing (`TelemetryService.cs`)

**Purpose**: Activity-based distributed tracing with OpenTelemetry

**Key Components**:
- `ITelemetryService` interface: Public contract for tracing operations
- `IOperationScope` interface: Activity lifecycle management
- `TelemetryService` implementation: ActivitySource and Meter management

**How It Works**:

```
ActivitySource creates Activity objects
         ↓
IOperationScope wraps Activity lifecycle
         ↓
Tags added for searchability (customer.id, order.total, etc.)
         ↓
Exceptions recorded with stack traces
         ↓
Metrics recorded on success
         ↓
Activity disposed → Exported to OTLP collector
```

**Usage Pattern**:

```csharp
public class CreateOrderCommandHandler
{
    private readonly ITelemetryService _telemetry;

    public async Task<OrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken ct)
    {
        // Start root operation activity
        using var scope = _telemetry.StartOperation("CreateOrder", "command");
        scope.SetTag("customer.id", command.CustomerId);
        scope.SetTag("order.items_count", command.Items.Count);

        try
        {
            // Child operation 1: Validation
            ValidateOrder(command, scope);

            // Child operation 2: Fetch data
            var customer = await FetchCustomerAsync(command, scope, ct);

            // Child operation 3: Business logic
            var orderId = await CreateOrderAsync(command, customer, scope, ct);

            // Mark success and record metrics
            scope.MarkSucceeded();
            _telemetry.RecordMetric("orders.created", 1, new()
            {
                { "customer_id", command.CustomerId },
                { "amount", command.Total }
            });

            return new OrderResult { OrderId = orderId };
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }

    private void ValidateOrder(CreateOrderCommand command, IOperationScope scope)
    {
        // Nested operation for detailed tracing
        using var childScope = _telemetry.StartOperation(
            "ValidateOrder", "validation");

        if (command.Items.Count == 0)
        {
            childScope.MarkFailed("Empty order");
            throw new BusinessRuleViolationException("Order must have items");
        }

        childScope.MarkSucceeded();
    }
}
```

**Activity Attributes**:

Every operation automatically includes:
- `operation.name`: Operation name ("CreateOrder")
- `operation.type`: Operation type ("command", "query", "mutation", "event")
- `operation.timestamp`: UTC timestamp
- `operation.duration_ms`: Duration in milliseconds
- Custom tags: `SetTag("key", value)` calls

**Integration with Serilog**:

When an operation creates an Activity, Serilog automatically includes:
- `{SpanId}`: OpenTelemetry span ID
- `{TraceId}`: OpenTelemetry trace ID
- `{ParentSpanId}`: Parent span ID

**Example Log with Trace**:

```
2024-01-15 10:30:45.123 [Information] CreateOrder completed successfully
  TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
  SpanId: 00f067aa0ba902b7
  ParentSpanId: 9a8c5fb31d05e1e1
  customer.id: "550e8400-e29b-41d4-a716-446655440000"
  order.items_count: 3
  operation.duration_ms: 245
```

### 3. Resilience Policies (`ResiliencePolicy.cs`)

**Purpose**: Production-grade resilience for HTTP clients

**Policies Implemented**:

```csharp
// Applied in order: Timeout → Bulkhead → CircuitBreaker → Retry

GetHttpRetryPolicy()
  ├─ Retries: 3 attempts
  ├─ Status codes: 408, 429, 503, 504
  ├─ Backoff: 100ms, 400ms, 1600ms (exponential)
  └─ Jitter: ±20% randomization to prevent thundering herd

GetHttpCircuitBreakerPolicy()
  ├─ Failure threshold: 3 failures
  ├─ Sampling window: 30 seconds
  ├─ Open duration: 30 seconds (auto-recovery)
  └─ Half-open: Gradually retries failed endpoint

GetHttpBulkheadPolicy()
  ├─ Max parallel requests: CPU count * 2
  ├─ Queue depth: 100
  └─ Rejection: Returns 429 when full

GetHttpTimeoutPolicy()
  ├─ Timeout: 30 seconds (configurable)
  ├─ Logging: Timeout events logged
  └─ Cancellation: Propagates CancellationToken

GetCombinedHttpPolicy()
  └─ Wraps all policies in order:
       Timeout → Bulkhead → CircuitBreaker → Retry
```

**Usage**:

```csharp
// In ServiceCollectionExtensions.cs or Program.cs
services.AddHttpClient<ExternalServiceClient>()
    .AddResiliencePolicies()
    .WithTimespan(TimeSpan.FromSeconds(30));

// In client code (automatic via DI)
public class ExternalServiceClient
{
    private readonly HttpClient _httpClient; // Already has policies applied

    public async Task<ExternalData> GetDataAsync(string id)
    {
        // Retry, timeout, bulkhead, and circuit breaker applied automatically
        var response = await _httpClient.GetAsync($"/api/data/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsJsonAsync<ExternalData>();
    }
}
```

**Policy Flow Diagram**:

```
Request → Timeout (30s limit)
            ↓
          Bulkhead (concurrent limit)
            ↓
          Circuit Breaker (failure tracking)
            ↓
          Retry Logic (3 attempts max)
            ↓
        External Service
            ↓
        Success → Return
        Failure → Retry (with backoff)
        Circuit Open → Return 503
        Timeout → Return timeout exception
```

### 4. Database Migrations (`DbUpMigrationService.cs`)

**Purpose**: Versioned database schema management

**Structure**:

```
Templates.Infrastructure/Persistence/Migrations/
├── Scripts/
│   ├── 001-Initial-Schema.sql
│   ├── 002-Add-Auditing-Tables.sql
│   ├── 003-Add-Messaging-Table.sql
│   └── 999-Seed-Reference-Data.sql
├── DbUpMigrationService.cs
└── MigrationExtensions.cs
```

**Migration Conventions**:

- **Naming**: `[sequence]-[Description].sql` (e.g., `001-Initial-Schema.sql`)
- **Sequence**: Executed in numeric order
- **Idempotency**: Each script should be idempotent (safe to run multiple times)
- **Transactions**: Each script runs in its own transaction (default DbUp behavior)
- **Version Tracking**: DbUp tracks in `SchemaVersions` table

**Usage**:

```csharp
// In Program.cs
var app = builder.Build();

// Run migrations at startup
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    await app.MigrateAsync();
}

await app.RunAsync();
```

**Example Migration Script** (001-Initial-Schema.sql):

```sql
-- Create database if needed
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'TemplatesDb')
    CREATE DATABASE [TemplatesDb];

USE [TemplatesDb];

-- Orders table
IF NOT EXISTS (SELECT 1 FROM sys.objects 
    WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') 
    AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [CustomerId] UNIQUEIDENTIFIER NOT NULL,
        [OrderNumber] NVARCHAR(50) NOT NULL UNIQUE,
        [Total] DECIMAL(18, 2) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedAt] DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE NONCLUSTERED INDEX [IX_Orders_CustomerId] 
        ON [dbo].[Orders]([CustomerId]);
END;
```

**API**:

```csharp
// Check migration status
var migrationService = serviceProvider.GetRequiredService<IDatabaseMigrationService>();
bool hasPending = await migrationService.HasPendingMigrationsAsync();
var applied = await migrationService.GetAppliedMigrationsAsync();

// Run migrations (throws on failure)
await migrationService.MigrateAsync();
```

### 5. Models

#### HealthCheckResponse

```csharp
public record HealthCheckResponse(
    string Status,                                    // "healthy", "degraded", "unhealthy"
    Dictionary<string, DependencyHealthStatus> Checks, // Per-dependency status
    DateTime Timestamp,                               // UTC timestamp
    int? HealthScore);                               // 0-100, null if unhealthy

public record DependencyHealthStatus(
    string Status,                                   // "healthy", "degraded", "unhealthy"
    string Message,                                  // Human-readable status
    long? ResponseTimeMs,                            // Milliseconds to respond
    Dictionary<string, object>? Details);            // Additional metadata
```

#### Example Health Response

```json
{
  "status": "healthy",
  "checks": {
    "database": {
      "status": "healthy",
      "message": "Connected to SQL Server",
      "responseTimeMs": 12,
      "details": {
        "connectionString": "Server=tcp:localhost,1433;Database=TemplatesDb"
      }
    },
    "cache": {
      "status": "healthy",
      "message": "Redis connection pool healthy",
      "responseTimeMs": 3,
      "details": null
    },
    "messaging": {
      "status": "healthy",
      "message": "RabbitMQ broker accessible",
      "responseTimeMs": 8,
      "details": null
    }
  },
  "timestamp": "2024-01-15T10:30:45.123Z",
  "healthScore": 100
}
```

## Integration Guide

### Step 1: Register Services (Program.cs)

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Phase 1: Security & Auth
builder.Services.AddSharedSecurity();
builder.Services.AddSharedAuth(builder.Configuration);

// Phase 2: Observability & Resilience
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddDatabaseMigrations();

// HTTP clients with resilience
builder.Services.AddHttpClient<ExternalServiceClient>()
    .AddResiliencePolicies();

var app = builder.Build();

// Middleware: Exception handling first
app.UseExceptionHandling();

// Security middleware
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapHealthEndpoints();
app.MapOrderEndpoints();

// Migrations
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    await app.MigrateAsync();
}

await app.RunAsync();
```

### Step 2: Add Telemetry to Domain Handlers

See [CreateOrderCommandHandlerWithTelemetryExample.cs](../src/Templates.Application/Features/Orders/Create/CreateOrderCommandHandlerWithTelemetryExample.cs) for complete pattern.

### Step 3: Configure OpenTelemetry Export

Add to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "ServiceName": "Templates.Api",
    "ServiceVersion": "1.0.0",
    "ExporterType": "otlp",
    "OtlpExporterEndpoint": "http://localhost:4317",
    "SamplingProbability": 1.0
  },
  "HealthChecks": {
    "DatabaseTimeout": "00:00:05",
    "CacheTimeout": "00:00:03"
  }
}
```

### Step 4: Configure Serilog with Trace Enrichment

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithTraceContext()  // Adds TraceId, SpanId
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

## Testing

### Integration Test Examples

See [HealthCheckAndOrderEndpointTests.cs](../tests/Templates.IntegrationTests/Examples/HealthCheckAndOrderEndpointTests.cs)

**Key Test Categories**:

1. **Health Endpoints**
   - Liveness always returns 200
   - Readiness depends on database
   - Detailed includes all dependency statuses

2. **Error Handling**
   - 404 EntityNotFoundException
   - 400 ValidationException
   - 409 ConflictException
   - 500 Unhandled exceptions

3. **Resilience**
   - Requests complete within timeout
   - Circuit breaker opens after threshold
   - Retry succeeds on transient failures

4. **Authentication**
   - Protected endpoints return 401 without auth
   - Token validation works correctly

## Monitoring & Observability

### Kubernetes Probes

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

### Grafana Dashboard Queries

```promql
# Request duration
histogram_quantile(0.95,
  rate(Templates_Application_request_duration_seconds_bucket[5m])
)

# Error rate
rate(Templates_Application_errors_total[5m])

# Health check status
Templates_health_check_status{instance=~"pod-.*"}

# Circuit breaker state
Templates_Application_circuit_breaker_state
```

### Log Aggregation (ELK/Datadog)

```
// Search for slow operations (> 1 second)
operation.duration_ms:[1000 TO *]

// Find exceptions in specific feature
SourceContext:"Templates.Application.Features.Orders.*" AND @level:Error

// Trace specific user's operations
user.id:"550e8400-e29b-41d4-a716-446655440000"
```

## Troubleshooting

### Health Check Returns "Unhealthy"

1. Check database connection: `healthCheck.Checks["database"].Message`
2. Verify connection string in appsettings.json
3. Check database service is running
4. Review timeout configuration

### Missing Trace IDs in Logs

1. Verify Serilog enrichment with `Enrich.WithTraceContext()`
2. Check ActivitySource is initialized: `new ActivitySource("Templates.Application")`
3. Ensure OpenTelemetry exporter is configured

### Circuit Breaker Always Open

1. Check external service health
2. Review error threshold (default: 3 failures)
3. Check window duration (default: 30 seconds)
4. Use detailed health endpoint to diagnose

### Database Migration Failure

1. Check SQL Server connectivity
2. Verify scripts in `Migrations/Scripts/` folder
3. Check for syntax errors in migration scripts
4. Review `SchemaVersions` table to see applied migrations
5. Run migrations in isolation: `dotnet run -- migrate`

## Next Steps: Phase 3

- Docker containerization with health check CMD
- Kubernetes deployment manifests
- OpenTelemetry Collector configuration
- Grafana dashboard setup
- Load testing with distributed tracing
- Production deployment runbooks

## References

- [OpenTelemetry .NET Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Polly Resilience Policies](https://github.com/App-vNext/Polly)
- [DbUp Documentation](https://dbup.readthedocs.io/)
- [Serilog Structured Logging](https://serilog.net/)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
