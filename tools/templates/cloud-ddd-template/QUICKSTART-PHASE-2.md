# Phase 2: Quick Start Guide

## Installation & Setup (5 minutes)

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB, Docker, or Azure)
- Visual Studio Code or Visual Studio 2022

### Step 1: Clone & Configure

```bash
# Clone the template
git clone https://github.com/your-org/cloud-ddd-template.git
cd cloud-ddd-template

# Set database connection (choose one)
# Option A: LocalDB (Windows only)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\\mssqllocaldb;Database=TemplatesDb;Trusted_Connection=true;"

# Option B: Docker SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrongPassword123!" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:latest

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=TemplatesDb;User Id=sa;Password=YourStrongPassword123!;"

# Option C: Azure SQL Database
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=TemplatesDb;Persist Security Info=False;User ID=sqladmin;Password=YourPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### Step 2: Build & Migrate

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run database migrations
cd src/Templates.Api
dotnet run -- migrate

# Or start the application directly (migrations run on startup)
dotnet run
```

### Step 3: Verify Installation

```bash
# In another terminal
curl -X GET http://localhost:5000/health/live
# Expected: HTTP 200 OK

curl -X GET http://localhost:5000/health/ready
# Expected: HTTP 200 OK

curl -X GET http://localhost:5000/health/detailed
# Expected: HTTP 200 OK with detailed health JSON
```

## Health Check Endpoints

### Liveness Probe (Kubernetes)

```bash
curl -X GET http://localhost:5000/health/live -w "\n%{http_code}\n"
```

**Response**: `200` (always, unless process hung)

**Use Case**: Kubernetes will restart container if this fails 3 times

### Readiness Probe (Kubernetes)

```bash
curl -X GET http://localhost:5000/health/ready -w "\n%{http_code}\n"
```

**Response**: `200` (if database healthy) or `503` (if unhealthy)

**Use Case**: Kubernetes removes from load balancer if this fails

### Detailed Health Status

```bash
curl -X GET http://localhost:5000/health/detailed | jq .
```

**Response**:
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
      "message": "Cache service available",
      "responseTimeMs": 3,
      "details": null
    },
    "messaging": {
      "status": "healthy",
      "message": "Message broker available",
      "responseTimeMs": 8,
      "details": null
    }
  },
  "timestamp": "2024-01-15T10:30:45.123Z",
  "healthScore": 100
}
```

## Distributed Tracing

### Adding Telemetry to Your Handler

```csharp
using Templates.Application.Common.Telemetry;

public class CreateOrderCommandHandler
{
    private readonly ITelemetryService _telemetry;

    public CreateOrderCommandHandler(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public async Task<Result> HandleAsync(CreateOrderCommand cmd)
    {
        // Start operation
        using var scope = _telemetry.StartOperation("CreateOrder", "command");
        scope.SetTag("customer.id", cmd.CustomerId);
        scope.SetTag("order.total", cmd.Total);

        try
        {
            // Your business logic
            var order = await _createOrderAsync(cmd);

            scope.MarkSucceeded();
            _telemetry.RecordMetric("orders.created", 1);
            return Result.Success(order);
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }
}
```

### Viewing Traces in Logs

Logs automatically include trace context:

```bash
# View logs with trace IDs
dotnet run | grep -E "TraceId|SpanId|order"

# Output:
# 2024-01-15 10:30:45.123 [Information] CreateOrder started
#   TraceId: 4bf92f3577b34da6a3ce929d0e0e4736
#   SpanId: 00f067aa0ba902b7
#   customer.id: "550e8400-e29b-41d4-a716-446655440000"
```

### Configuring OpenTelemetry Export (Advanced)

Add to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "ExporterType": "otlp",
    "OtlpExporterEndpoint": "http://localhost:4317"
  }
}
```

Set environment variable (override):

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://my-collector:4317
```

## Resilience Policies

### How They Work

HTTP clients automatically get resilience:

```csharp
// In dependency injection
services.AddHttpClient<ExternalApiClient>()
    .AddResiliencePolicies();  // ← Automatic resilience

// In your client (transparent)
public class ExternalApiClient
{
    private readonly HttpClient _client; // Already has policies

    public async Task<Data> GetDataAsync(string id)
    {
        // Automatically retries 3 times on transient failures
        // Has circuit breaker to prevent cascading failures
        // Has timeout (30s default)
        // Has bulkhead to limit parallel requests
        var response = await _client.GetAsync($"/api/data/{id}");
        return await response.Content.ReadAsJsonAsync<Data>();
    }
}
```

### Configuring Policies

Edit `appsettings.json`:

```json
{
  "Resilience": {
    "HttpClient": {
      "TimeoutSeconds": 30,
      "CircuitBreakerThreshold": 3,
      "CircuitBreakerWindowSeconds": 30,
      "RetryCount": 3,
      "InitialBackoffMs": 100,
      "MaxParallelRequests": 0,
      "BulkheadQueueDepth": 100
    }
  }
}
```

**Settings**:
- `TimeoutSeconds`: Max time per request (default: 30)
- `CircuitBreakerThreshold`: Failures before opening (default: 3)
- `RetryCount`: Retry attempts (default: 3)
- `MaxParallelRequests`: 0 = CPU count * 2

## Database Migrations

### Creating a New Migration

1. Create SQL file in `src/Templates.Infrastructure/Persistence/Migrations/Scripts/`:

```sql
-- 005-Add-Inventory-Table.sql
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Inventory]'))
BEGIN
    CREATE TABLE [dbo].[Inventory] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [ProductId] UNIQUEIDENTIFIER NOT NULL,
        [Quantity] INT NOT NULL,
        [UpdatedAt] DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
    );
END;
```

2. Add to `.csproj` as embedded resource (automatic if placed in Scripts folder):

```xml
<ItemGroup>
    <EmbeddedResource Include="Persistence/Migrations/Scripts/*.sql" />
</ItemGroup>
```

3. Run application (migrations auto-execute on startup):

```bash
dotnet run
```

4. Verify in database:

```sql
SELECT * FROM SchemaVersions;
-- Shows: 005-Add-Inventory-Table.sql applied at UTC time
```

### Rollback Strategy

DbUp doesn't support rollback, so create compensating migrations:

```sql
-- 006-Fix-Inventory-Schema.sql (compensating for 005)
ALTER TABLE [dbo].[Inventory] 
ADD [Warehouse] NVARCHAR(50) NOT NULL DEFAULT 'Main';
```

## Running Tests

### Integration Tests

```bash
# Run all integration tests
dotnet test tests/Templates.IntegrationTests/

# Run specific test
dotnet test tests/Templates.IntegrationTests/ \
  -t HealthCheckEndpointTests.HealthDetailed_ReturnsDetailedStatus

# With detailed output
dotnet test --verbosity=detailed tests/Templates.IntegrationTests/
```

### Test Examples

Tests demonstrate:
- ✅ Health endpoint responses
- ✅ Error handling (404, 401, 400)
- ✅ Standard error response format
- ✅ Resilience patterns

## Troubleshooting

### Database Connection Failed

```bash
# Verify connection string
dotnet user-secrets list

# Test connection manually
sqlcmd -S (localdb)\mssqllocaldb -d TemplatesDb -Q "SELECT 1"

# Fix: Update connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "correct-string"
```

### Health Check Returns Unhealthy

```bash
# Check database directly
curl http://localhost:5000/health/detailed | jq '.checks.database'

# Fix: Ensure database is running
# For Docker: docker ps | grep mssql
# For LocalDB: sqllocaldb info
```

### Migrations Not Running

```bash
# Check applied migrations
docker exec -it mssql /opt/mssql-tools/bin/sqlcmd -S localhost \
  -U sa -P YourPassword -Q "SELECT * FROM SchemaVersions"

# Re-run migrations
dotnet run -- migrate

# Check logs for errors
```

### Timeout on First Request

```bash
# Normal - first request includes migrations
# Subsequent requests should be <100ms

# To pre-run migrations:
cd src/Templates.Api
dotnet run -- migrate
# Then stop and restart normally
```

## Development Workflow

### Hot Reload

```bash
# Run with hot reload (detects code changes)
dotnet watch run

# Any file change automatically rebuilds and restarts
# Migrations still run on startup
```

### Database Reset (Development Only)

```bash
# Drop and recreate database
sqlcmd -S (localdb)\mssqllocaldb -Q "DROP DATABASE TemplatesDb"

# Migrations will recreate on next run
dotnet run
```

### Debugging

```bash
# Run with debugger attached
dotnet run

# Set breakpoint in VS Code:
# 1. Open file
# 2. Click left of line number
# 3. F5 to start debugging
# 4. Breakpoint will trigger
```

## Common Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Run
dotnet run

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Staging dotnet run

# Run migrations only
dotnet run -- migrate

# Format code
dotnet format

# Check for errors
dotnet build --no-restore

# Restore packages
dotnet restore

# Pack as NuGet
dotnet pack --configuration Release
```

## Next Steps

1. ✅ **Immediate**: Configure database and run migrations
2. ✅ **Setup**: Add JWT token configuration
3. ✅ **Development**: Add your domain models
4. ✅ **Features**: Implement handlers with telemetry
5. ✅ **Testing**: Add integration tests for endpoints
6. ⬜ **Phase 3**: Deploy to Kubernetes

## Documentation

- 📖 [Phase 2 Implementation Guide](./docs/guides/PHASE-2-OBSERVABILITY-AND-RESILIENCE.md)
- 📖 [Phase 2 Configuration Reference](./docs/guides/PHASE-2-CONFIGURATION.md)
- 📖 [Phase 2 Completion Summary](./docs/PHASE-2-COMPLETION-SUMMARY.md)
- 📖 [Security Guidelines](./docs/guides/SECURITY-GUIDELINES.md)

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review Phase 2 implementation guide
3. Check example handlers and tests
4. Open GitHub issue with:
   - Error message
   - Steps to reproduce
   - Environment details (.NET version, OS)

---

**Last Updated**: 2024-01-15
**Version**: 1.0.0
**Status**: Production Ready
