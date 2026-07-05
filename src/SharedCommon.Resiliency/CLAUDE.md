# SharedCommon.Resiliency

Polly resilience policies: retry, circuit breaker, timeout, bulkhead.
Pre-configured policies for common scenarios.

## API Surface

- `ResiliencyOptions` — per-policy configuration
- `AddSharedResiliency(IConfiguration)` — DI registration
- `IResiliencyPolicyProvider` — get policies by name
- Pre-built policies: `RetryPolicy`, `CircuitBreakerPolicy`, `TimeoutPolicy`
- `HttpClientBuilderExtensions` — apply policies to named HTTP clients

## Rules

**Must:**
- All retry policies use exponential backoff with jitter
- Circuit breakers log state transitions (Closed → Open → HalfOpen → Closed)
- Timeout policies propagate CancellationToken correctly
- Policies configurable via `ResiliencyOptions` (no hardcoded values)

**Forbidden:**
- Infinite retry loops
- Swallowing exceptions in policy `onFallback` handlers
- Retry on non-transient errors (e.g., 400 Bad Request)
- Circuit breakers without monitoring/alerting consideration

## Design Decisions

Polly v8 pipelines used (not legacy Policy.WrapAsync).
HTTP client policies applied via `Microsoft.Extensions.Http.Resilience`.

## Test Strategy

**Unit Tests** (`tests/SharedCommon.Resiliency.UnitTests`):
- Configuration defaults and validation: `ResiliencyOptionsTests` (5 tests)
- Behavioral tests: `ResiliencyBehaviorTests` (12 tests)
  - Policy registration: default pipeline with all three strategies, retry-only, timeout-only pipelines
  - Policy retrieval: caching behavior, registry injection
  - Policy execution: successful operations, configuration application
  - Configuration persistence: custom retry/circuit-breaker/timeout settings
  - Multiple executions: consistency across calls
  - Logger injection validation
  - Pipeline constant names verification

Test the following scenarios:
- Retry triggers on transient failures
- Circuit breaker opens after threshold
- Timeout cancels long-running operations
- Polly's `SimulatedException` for unit testing
- Configuration binding from `appsettings.json`
- Singleton behavior for provider and registry

## Extension Points

- Custom `IResiliencyPolicy` implementations
- Policy chaining via `ResiliencePipeline`
