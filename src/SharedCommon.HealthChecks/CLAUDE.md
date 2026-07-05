# SharedCommon.HealthChecks

Health check abstractions and common checks for infrastructure dependencies.
Exposes `/health/live` and `/health/ready` endpoints.

## API Surface

- `AddSharedHealthChecks(IConfiguration)` — registers standard checks
- `UseSharedHealthEndpoints()` — maps `/health/live` and `/health/ready`
- Built-in checks: Redis, Kafka, SQL Server, external HTTP
- `IHealthCheckReporter` — structured health report output

## Rules

**Must:**
- `/health/live` — fast, no external calls (process alive check only)
- `/health/ready` — checks all critical dependencies (Redis, DB, etc.)
- Health checks must timeout within 5 seconds
- Log degraded/unhealthy transitions at Warning/Error
- Health check names match infrastructure service names

**Forbidden:**
- Business logic in health checks
- Health checks that take > 5 seconds
- Returning sensitive infrastructure details in public health responses
- Blocking async health checks with `.Result`

## Design Decisions

Live vs Ready split follows Kubernetes probe semantics.
Ready checks all dependencies; live only checks if the process is responsive.

## Test Strategy

- Unit test custom health check logic with mocked dependencies
- Verify degraded and unhealthy states produce correct HTTP status codes
- Integration tests require actual infrastructure (Redis, etc.)

## Extension Points

- Custom `IHealthCheck` implementations
- Custom health response writer via `IHealthCheckResponseWriter`
