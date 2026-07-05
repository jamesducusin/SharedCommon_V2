# Understanding the Architecture

## Mental Model

SharedCommon is not a monolith — it's a collection of independent packages that consuming services compose together.

Think of each package as a mini-library:
- **Install only what you need** — your service doesn't have to take all packages
- **Each package does one thing** — Caching caches, Auth authenticates, etc.
- **They share a common foundation** — SharedCommon.Core

## The Dependency Hierarchy

```
Your Service
  ↓ installs
SharedCommon.Auth + SharedCommon.Caching + SharedCommon.Logging
  ↓ all depend on
SharedCommon.Core
  (no outward dependencies)
```

Core is the only package with no dependencies. Everything else can depend on Core, but not on each other (unless explicitly allowed in dependency-rules.md).

## How Context Flows

```
HTTP Request
  → CorrelationIdMiddleware (assigns correlation ID)
  → RequestLoggingMiddleware (logs with correlation ID)
  → Auth middleware (validates token)
  → Controller (thin, delegates to service)
  → Application Service (use case logic)
    → Cache (check cache first)
    → Repository (if cache miss)
  → ResponseBuilder (wraps result)
  → HTTP Response
```

Every step uses the same CorrelationId for end-to-end traceability.

## Clean Architecture in Practice

Don't think of layers as directories. Think of them as rings:

- **Innermost ring (Core):** Domain types, Result<T>, base interfaces — no dependencies
- **Middle ring:** Application services — depend on Core, use interfaces
- **Outer ring:** Infrastructure — implement interfaces from middle ring, perform I/O

The rule: dependencies point inward. Infrastructure depends on Application, Application depends on Core. Never the other way.

## Why This Matters

If you put a Redis call directly in a controller:
- You can't unit test the controller without Redis
- You can't swap Redis for in-memory in tests
- The controller now knows about infrastructure details

If you put it behind `ICacheService`:
- Controller is testable with a mock
- You can swap implementations without changing the controller
- The infrastructure concern is isolated

## Further Reading

- [docs/architecture/layering.md](../architecture/layering.md) — exact layer definitions
- [docs/architecture/dependency-rules.md](../architecture/dependency-rules.md) — what can reference what
- [docs/adr/](../adr/) — why decisions were made
