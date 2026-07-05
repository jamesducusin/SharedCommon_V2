# [ServiceName] Microservice

[One-line description of this service's business purpose.]

## Responsibilities

- [Core responsibility 1]
- [Core responsibility 2]

## SharedCommon Packages Used

- SharedCommon.Core
- SharedCommon.Logging
- SharedCommon.Auth
- SharedCommon.Middlewares
- SharedCommon.ResponseBuilder
- SharedCommon.HealthChecks

## Architecture

This service follows Clean Architecture:
- `Api/` — controllers, minimal APIs, gRPC services
- `Application/` — use cases, commands, queries
- `Domain/` — entities, value objects, domain services
- `Infrastructure/` — repositories, external service clients

## Configuration

All configuration via `appsettings.json` + environment variables (no hardcoded values).

## Rules

- Follow root CLAUDE.md
- Follow each SharedCommon package's CLAUDE.md for usage rules
- All use cases return `Result<T>`
- All async methods accept CancellationToken

## Test Strategy

- Unit tests for domain logic and application services
- Integration tests via `WebApplicationFactory<Program>`
- Architecture tests enforce layering rules
