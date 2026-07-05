# System Architecture Overview

## Philosophy

SharedCommon is a collection of independent, enterprise-grade .NET packages that provide infrastructure concerns to consuming services. Each package is:

- **Independently installable** — consumers install only what they need
- **Configuration-driven** — all behavior controlled via `appsettings.json` and `IOptions<T>`
- **Observable by default** — logging, tracing, and metrics built in
- **Secure by default** — no secrets in code, least-privilege, input validation

## Package Taxonomy

| Layer | Packages | Purpose |
|-------|----------|---------|
| Core | SharedCommon.Core | Shared abstractions, result types, base interfaces |
| Observability | SharedCommon.Logging, SharedCommon.Observability | Structured logging, tracing, metrics |
| Security | SharedCommon.Security, SharedCommon.Auth | Security headers, rate limiting, JWT authentication |
| Infrastructure | SharedCommon.Caching, SharedCommon.Messaging, SharedCommon.Cloud, SharedCommon.Storage | Redis, Kafka, cloud abstractions, file storage |
| API | SharedCommon.Grpc, SharedCommon.GraphQL, SharedCommon.Middlewares, SharedCommon.ApiVersioning | Protocol adapters, pipeline, versioning |
| Utilities | SharedCommon.Utilities, SharedCommon.Validation, SharedCommon.HealthChecks | Helpers, validation, health |
| Resilience | SharedCommon.Resiliency | Polly policies, circuit breakers |
| Responses | SharedCommon.ResponseBuilder | Standardized HTTP response envelopes |
| Operations | SharedCommon.Auditing, SharedCommon.BackgroundJobs | Audit trail, job scheduling |
| Platform | SharedCommon.MultiTenancy, SharedCommon.FeatureFlags | Multi-tenancy, feature flag evaluation |

## Dependency Direction

```
SharedCommon.Core (no outward dependencies)
        ↑
All other packages depend on Core, not each other (unless explicitly documented in ADRs)
```

## Technology Choices

- **Runtime:** .NET 8 (LTS)
- **DI:** Microsoft.Extensions.DependencyInjection
- **Logging:** Serilog via Microsoft.Extensions.Logging abstraction
- **Tracing:** OpenTelemetry
- **Caching:** HybridCache (IMemoryCache + IDistributedCache/Redis)
- **Testing:** xUnit + NSubstitute + NetArchTest + BenchmarkDotNet

## Cross-Cutting Concerns

Every package must implement:
1. `IServiceCollection` extension method for registration
2. `IOptions<T>` configuration binding
3. `ILogger<T>` injection
4. `CancellationToken` on all async public methods

See: docs/architecture/layering.md
See: docs/architecture/dependency-rules.md
