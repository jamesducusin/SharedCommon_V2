# Layering Rules

## Clean Architecture Mapping

```
┌─────────────────────────────────────────┐
│           Presentation Layer             │  ← Controllers, Minimal APIs, gRPC Services
│   (SharedCommon.Middlewares, .Grpc,      │
│    .GraphQL, .ResponseBuilder,           │
│    .ApiVersioning, .MultiTenancy)        │
├─────────────────────────────────────────┤
│           Application Layer             │  ← Use cases, orchestration
│   (SharedCommon.Validation,             │
│    .FeatureFlags, .Auditing,            │
│    .BackgroundJobs)                     │
├─────────────────────────────────────────┤
│             Domain Layer                │  ← Business rules, entities
│   (SharedCommon.Core)                   │
├─────────────────────────────────────────┤
│          Infrastructure Layer           │  ← I/O, persistence, external services
│   (SharedCommon.Caching, .Messaging,    │
│    .Cloud, .Storage, .Auth, .Security,  │
│    .Resiliency, .HealthChecks)          │
└─────────────────────────────────────────┘
```

## Dependency Rules

- Presentation → Application: **Allowed**
- Application → Domain: **Allowed**
- Infrastructure → Domain: **Allowed** (implements abstractions)
- Domain → anything above: **FORBIDDEN**
- Presentation → Infrastructure directly: **FORBIDDEN**
- Application → Infrastructure directly: **FORBIDDEN** (use abstractions)

## Package-Level Rules

### SharedCommon.Core
- Zero external package dependencies
- Contains: Result<T>, IEntity, base interfaces, value objects
- Cannot reference any other SharedCommon package

### SharedCommon.Logging / SharedCommon.Observability
- Depends on: Core, Microsoft.Extensions.Logging abstractions
- Cannot reference: Caching, Auth, Messaging

### SharedCommon.Auth / SharedCommon.Security
- Depends on: Core, Logging
- Cannot reference: Caching (no implicit caching in auth)

### SharedCommon.Caching
- Depends on: Core, Logging
- Cannot reference: Auth, Security (cache should not know about auth)

### SharedCommon.Middlewares
- Depends on: Core, Logging, ResponseBuilder
- May reference: Auth, Validation (for middleware that uses them)
- Cannot contain business logic

## Enforcement

These rules are enforced by:
1. `tests/SharedCommon.ArchitectureTests/LayeringTests.cs`
2. `.claude/hooks/validate-architecture.ps1`
3. `.claude/hooks/validate-dependencies.ps1`
