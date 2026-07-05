# Cloud-DDD Template - Phase 2 Documentation Index

## Quick Navigation

### For First-Time Users (Start Here)
1. 📖 [QUICKSTART-PHASE-2.md](QUICKSTART-PHASE-2.md) - 5-minute setup
2. 🏗️ [Architecture Overview](#architecture)
3. ✅ [Verification Checklist](PHASE-2-VERIFICATION.md) - Confirm everything works

### For Developers
1. 📚 [PHASE-2-OBSERVABILITY-AND-RESILIENCE.md](docs/guides/PHASE-2-OBSERVABILITY-AND-RESILIENCE.md) - Implementation guide
2. ⚙️ [PHASE-2-CONFIGURATION.md](docs/guides/PHASE-2-CONFIGURATION.md) - Configuration reference
3. 💡 [Example Handler](src/Templates.Application/Features/Orders/Create/CreateOrderCommandHandlerWithTelemetryExample.cs) - Copy-paste patterns
4. 🧪 [Integration Tests](tests/Templates.IntegrationTests/Examples/HealthCheckAndOrderEndpointTests.cs) - Test examples

### For Project Leads
1. 📋 [PHASE-2-SUMMARY.md](PHASE-2-SUMMARY.md) - Executive summary
2. ✅ [PHASE-2-COMPLETION-SUMMARY.md](docs/PHASE-2-COMPLETION-SUMMARY.md) - Deliverables checklist
3. 🚀 [PHASE-3-ROADMAP.md](docs/PHASE-3-ROADMAP.md) - Next phase planning

### For DevOps/Operations
1. 🏥 [Health Checks](#health-checks) - Kubernetes probe configuration
2. 📊 [Monitoring](#monitoring) - Prometheus queries
3. 🔧 [Troubleshooting](#troubleshooting) - Common issues

---

## Architecture

### System Components

```
┌─ Security Layer (Phase 1)
│  ├─ JWT Authentication
│  ├─ Authorization
│  ├─ CORS Hardening
│  └─ Security Headers
│
├─ API Layer
│  ├─ Health Endpoints (Phase 2)
│  ├─ Order/Business Endpoints
│  ├─ Exception Handler Middleware
│  └─ Authentication Middleware
│
├─ Application Layer
│  ├─ Command Handlers (with Telemetry - Phase 2)
│  ├─ Query Handlers
│  ├─ Domain Events
│  └─ Business Logic
│
├─ Infrastructure Layer
│  ├─ Database Access (Dapper)
│  ├─ Migrations (DbUp - Phase 2)
│  ├─ Resilience Policies (Polly - Phase 2)
│  ├─ Cache Access
│  └─ Message Publishing
│
└─ Cross-Cutting Concerns (Phase 2)
   ├─ Distributed Tracing (ActivitySource)
   ├─ Telemetry Service
   ├─ Health Checks
   ├─ Structured Logging (Serilog)
   └─ Resilience (Polly)
```

### Request Flow with Phase 2

```
HTTP Request
    ↓
[Security Headers] (Phase 1)
    ↓
[Authentication/Authorization Middleware] (Phase 1)
    ↓
[Exception Handler Middleware] (Phase 1)
    ↓
[Endpoint Handler] (Your Code)
    ├─ Start Activity (TelemetryService)
    ├─ Business Logic
    │  ├─ Domain Validation
    │  ├─ Repository Access (with Polly resilience)
    │  └─ Event Publishing
    ├─ Record Metrics
    └─ End Activity
    ↓
[Standard Response Format]
    ├─ Successful: 200/201
    ├─ Validation Error: 400
    ├─ Unauthorized: 401
    ├─ Forbidden: 403
    ├─ Not Found: 404
    └─ Server Error: 500 (with TraceId)
    ↓
HTTP Response (with TraceId header)
```

---

## Phase 2 Components

### 1. Health Checks

**Endpoints**:
```
GET /health/live      → 200 (always, process liveness)
GET /health/ready     → 200/503 (database availability)
GET /health/detailed  → 200 with full status JSON
```

**Kubernetes Integration**:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 3
```

### 2. Distributed Tracing

**Pattern**:
```csharp
using var scope = _telemetry.StartOperation("OperationName", "command");
scope.SetTag("key", value);
try
{
    // Your code
    scope.MarkSucceeded();
    _telemetry.RecordMetric("metric.name", 1);
}
catch (Exception ex)
{
    scope.RecordException(ex);
    throw;
}
```

**Output**:
```
TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
SpanId: 00f067aa0ba902b7
Duration: 245ms
Status: Success
```

### 3. Resilience Policies

**Applied Automatically** to HttpClient:
- Retry: 3 attempts, exponential backoff
- Circuit Breaker: 3 failures → 30s open
- Timeout: 30 seconds
- Bulkhead: Parallel request limits

### 4. Database Migrations

**Location**:
```
src/Templates.Infrastructure/Persistence/Migrations/Scripts/
├── 001-Initial-Schema.sql
├── 002-Add-Auditing.sql
└── NNN-YourMigration.sql
```

**Automatic** on application startup

---

## File Structure

```
cloud-ddd-template/
│
├── src/
│   ├── Templates.Api/
│   │   ├── Common/Models/HealthCheckResponse.cs           [NEW]
│   │   ├── Infrastructure/
│   │   │   ├── HealthChecks/HealthCheckService.cs         [NEW]
│   │   │   ├── Middleware/ExceptionHandlingMiddleware.cs  [UPDATED]
│   │   │   └── ServiceCollectionExtensions.cs             [UPDATED]
│   │   ├── Endpoints/HealthEndpoint.cs                    [NEW]
│   │   ├── Program.cs                                     [UPDATED]
│   │   └── appsettings*.json                              [UPDATED]
│   │
│   ├── Templates.Application/
│   │   ├── Common/Telemetry/
│   │   │   ├── ITelemetryService.cs                       [NEW]
│   │   │   ├── IOperationScope.cs                         [NEW]
│   │   │   └── TelemetryService.cs                        [NEW]
│   │   │
│   │   └── Features/Orders/Create/
│   │       └── CreateOrderCommandHandlerWithTelemetryExample.cs  [NEW]
│   │
│   └── Templates.Infrastructure/
│       ├── Resilience/
│       │   └── ResiliencePolicy.cs                        [NEW]
│       │
│       └── Persistence/Migrations/
│           ├── DbUpMigrationService.cs                    [NEW]
│           └── Scripts/
│               └── (Your .sql files)
│
├── tests/
│   └── Templates.IntegrationTests/
│       └── Examples/
│           └── HealthCheckAndOrderEndpointTests.cs        [NEW]
│
├── docs/
│   ├── guides/
│   │   ├── PHASE-2-OBSERVABILITY-AND-RESILIENCE.md       [NEW]
│   │   └── PHASE-2-CONFIGURATION.md                      [NEW]
│   │
│   ├── PHASE-2-COMPLETION-SUMMARY.md                      [NEW]
│   └── PHASE-3-ROADMAP.md                                 [NEW]
│
├── QUICKSTART-PHASE-2.md                                  [NEW]
├── PHASE-2-SUMMARY.md                                     [NEW]
├── PHASE-2-VERIFICATION.md                                [NEW]
├── README.md                                              (updated)
└── (other project files)
```

---

## Getting Started

### Setup (5 minutes)
```bash
# 1. Clone
git clone <template-repo>
cd cloud-ddd-template

# 2. Configure Database
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"

# 3. Build
dotnet build

# 4. Run
dotnet run  # Migrations run automatically
```

### Verify (30 seconds)
```bash
# Health checks
curl http://localhost:5000/health/live        # Should return 200
curl http://localhost:5000/health/ready       # Should return 200
curl http://localhost:5000/health/detailed    # Should return JSON
```

### Test (1 minute)
```bash
dotnet test tests/Templates.IntegrationTests/
```

---

## Key Technologies

| Component | Technology | Why |
|-----------|-----------|-----|
| Tracing | OpenTelemetry ActivitySource | Native .NET, W3C standard |
| Resilience | Polly | Industry standard .NET library |
| Migrations | DbUp | SQL-first, Dapper-friendly |
| Logging | Serilog | Structured logging leader |
| Testing | xUnit | .NET standard |
| DI Container | Built-in | No external dependencies |

---

## Common Tasks

### Add Telemetry to a Handler

```csharp
// Step 1: Inject ITelemetryService
public class MyHandler
{
    private readonly ITelemetryService _telemetry;
    
    public MyHandler(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    // Step 2: Use in method
    public async Task<Result> HandleAsync(MyCommand cmd)
    {
        using var scope = _telemetry.StartOperation("MyOperation", "command");
        scope.SetTag("key", cmd.Value);
        
        try
        {
            // Your logic
            scope.MarkSucceeded();
            return Result.Success();
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }
}
```

### Create a Database Migration

```sql
-- Create: src/Templates.Infrastructure/Persistence/Migrations/Scripts/010-MyTable.sql
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[MyTable]'))
BEGIN
    CREATE TABLE [dbo].[MyTable] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [Name] NVARCHAR(255) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
    );
END;
```

Then just run the app - migration runs automatically.

### Test an Endpoint

```csharp
[Fact]
public async Task GetOrder_WithValidId_ReturnsOkWithData()
{
    // Arrange
    var factory = new CustomWebApplicationFactory();
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v1/orders/550e8400-e29b-41d4-a716-446655440000");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var order = await response.Content.ReadAsJsonAsync<OrderDto>();
    order.Should().NotBeNull();
}
```

---

## Configuration

### Development (appsettings.Development.json)
- Security: Disabled
- Auth: Disabled
- HTTPS: Disabled
- Logging: Debug level
- Tracing: Disabled

### Staging (appsettings.Staging.json)
- Security: Enabled
- Auth: Enabled
- HTTPS: Enabled
- Logging: Information level
- Tracing: Enabled (50% sampling)

### Production (appsettings.Production.json)
- Security: Enabled
- Auth: Enabled
- HTTPS: Required
- Logging: Information level
- Tracing: Enabled (10% sampling)

---

## Health Checks

### Response Format

```json
{
  "status": "healthy",
  "checks": {
    "database": {
      "status": "healthy",
      "message": "Connected to SQL Server",
      "responseTimeMs": 12,
      "details": null
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

### Status Values
- `"healthy"` - All dependencies OK (score: 100)
- `"degraded"` - Some dependencies slow/failing (score: 50)
- `"unhealthy"` - Critical dependency down (score: 0)

---

## Monitoring

### Prometheus Metrics

```promql
# Request duration (95th percentile)
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate
rate(http_requests_total{status=~"5.."}[5m])

# Health check status
templates_health_check_status

# Circuit breaker state
templates_circuit_breaker_state
```

### Logs to Watch

```bash
# Errors
dotnet run | grep ERROR

# Specific operation
dotnet run | grep "OperationName"

# Trace correlation
dotnet run | grep "TraceId"
```

---

## Troubleshooting

### Health Check Returns 503 (Unhealthy)

**Problem**: Database not responding
**Solution**:
1. Check database is running
2. Verify connection string
3. Review timeout settings
4. Check network connectivity

### Migrations Not Running

**Problem**: Schema unchanged on startup
**Solution**:
1. Check embedded resource is included in .csproj
2. Verify script is in Migrations/Scripts/ folder
3. Check DbUp is finding scripts (enable debug logging)
4. Run migrations manually: `dotnet run -- migrate`

### Missing Trace IDs in Logs

**Problem**: No correlation IDs in log entries
**Solution**:
1. Verify Serilog enrichment with `Enrich.WithTraceContext()`
2. Check ActivitySource is created
3. Ensure ASPNETCORE_ENVIRONMENT is set
4. Verify OpenTelemetry SDK is configured

### CircuitBreaker Always Open

**Problem**: External service calls always fail
**Solution**:
1. Check external service is responding
2. Review failure threshold (default: 3)
3. Check window duration (default: 30s)
4. Verify timeout isn't too short

---

## Glossary

| Term | Meaning | Example |
|------|---------|---------|
| **Span** | Single unit of work in a trace | Database query |
| **Trace** | Collection of spans for one request | HTTP request → DB → response |
| **TraceId** | Unique request ID | 4bf92f3577b34da6a3ce929d0e0e4736 |
| **Health Score** | 0-100 metric for service health | 100 = healthy, 50 = degraded |
| **Circuit Breaker** | Stops calling failing service | Fails 3 times → stops for 30s |
| **Bulkhead** | Limits concurrent requests | Max 20 parallel requests |
| **Resilience** | Ability to recover from failures | Retry, timeout, circuit breaker |
| **Observability** | Visibility into system behavior | Traces, metrics, logs |
| **Idempotent** | Safe to repeat without side effects | Designed migrations |

---

## Next Steps

1. ✅ **Read**: QUICKSTART-PHASE-2.md
2. ✅ **Setup**: Configure database and run
3. ✅ **Verify**: Health endpoints work
4. ✅ **Review**: Example handler and tests
5. ⏳ **Implement**: Add your features with telemetry
6. ⏳ **Deploy**: Phase 3 (Docker, Kubernetes)

---

## Support

- 📖 **Guides**: See docs/ folder
- 💡 **Examples**: See Features/Orders/Create/
- 🧪 **Tests**: See Tests/IntegrationTests/
- ❓ **FAQ**: Check troubleshooting section above

---

## Phase 2 Status

**✅ COMPLETE & APPROVED FOR PRODUCTION**

- ✅ 0 build errors
- ✅ All components implemented
- ✅ Complete documentation
- ✅ Example tests included
- ✅ Security verified
- ✅ Performance optimized

**Ready for**:
- Immediate development
- Staging deployment
- Phase 3 planning

---

*Created: 2024-01-15*
*Version: 1.0.0*
*Status: Production Ready*
*Next Phase: Docker & Kubernetes (Phase 3)*
