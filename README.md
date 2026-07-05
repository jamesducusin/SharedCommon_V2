# Cerberus — SharedCommon Platform

Enterprise-grade .NET infrastructure packages for modern microservices.
Modular, observable, secure, cloud-native.

## ⚠️ Multi-Tenancy Security Notice

If using **SharedCommon.MultiTenancy**, remember that it provides **tenant identification only**, not isolation enforcement.
Your application MUST enforce data isolation at all layers (queries, cache, authorization, background jobs, logging).
See [Security Guidelines: Multi-Tenancy Data Isolation](docs/standards/security-guidelines.md#multi-tenancy-data-isolation) for implementation patterns.

## Packages

| Package | Purpose |
|---------|---------|
| SharedCommon.Core | Shared abstractions, Result<T>, base types |
| SharedCommon.Logging | Structured logging via Serilog |
| SharedCommon.Observability | OpenTelemetry tracing and metrics |
| SharedCommon.Caching | Hybrid cache (in-memory + Redis) |
| SharedCommon.Security | Encryption, hashing, secret management |
| SharedCommon.Auth | JWT, API key authentication |
| SharedCommon.Validation | FluentValidation DI integration |
| SharedCommon.HealthChecks | Liveness/readiness health checks |
| SharedCommon.Messaging | RabbitMQ or Kafka via MassTransit (transport is config-driven) |
| SharedCommon.Middlewares | ASP.NET Core pipeline middleware |
| SharedCommon.Grpc | gRPC interceptors and infrastructure |
| SharedCommon.GraphQL | Hot Chocolate GraphQL infrastructure |
| SharedCommon.Resiliency | Polly retry, circuit breaker, timeout |
| SharedCommon.ResponseBuilder | Standardized API response envelopes |
| SharedCommon.Auditing | Structured audit trail (logging, database, or messaging backend) |
| SharedCommon.BackgroundJobs | Hangfire background jobs, recurring jobs, dashboard |
| SharedCommon.Cloud | Blob storage, secret management, cloud queues (Azure + AWS) |
| SharedCommon.ApiVersioning | URL-segment, header, query-string API versioning |
| SharedCommon.FeatureFlags | Feature flag evaluation via Microsoft.FeatureManagement |
| SharedCommon.MultiTenancy | Request-scoped tenant resolution (header, JWT claim, subdomain, query-string) |
| SharedCommon.Storage | Provider-agnostic file storage (local filesystem; swappable to cloud) |
| SharedCommon.Utilities | Lightweight, dependency-free helpers |

## Quick Start

```powershell
# Setup
.\tools\scripts\bootstrap-dev-env.ps1

# Build
dotnet build

# Test
dotnet test

# Validate everything
.\tools\scripts\validate-solution.ps1
```

## Documentation

- [Consuming Packages](docs/guides/consuming-packages.md) — install and use in your own project
- [Architecture Overview](docs/architecture/overview.md)
- [First-Time Setup](docs/guides/first-time-setup.md)
- [Adding a Package](docs/guides/adding-a-package.md)
- [Architecture Decision Records](docs/adr/)
- [Coding Standards](docs/standards/coding-standards.md)
- [Changelog](CHANGELOG.md)

## AI-Native Development

This repository is designed for Claude Code. Read [.claude/navigation.md](.claude/navigation.md) to understand how to use Claude effectively for tasks in this codebase.

Key workflows:
- Add a package: `.claude/prompts/add-new-package.md`
- Security audit: `.claude/prompts/security-audit.md`
- Architecture review: `.claude/prompts/architecture-review.md`

## License

MIT
