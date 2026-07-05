# SharedCommon.MultiTenancy

Multi-tenancy infrastructure: tenant resolution, scoped tenant context, and middleware.

## API Surface

- `ITenantContext` — request-scoped: `TenantId`, `TenantName`, `IsResolved`, `Properties`
- `ITenantResolver` — resolves `TenantInfo` from `HttpContext`; default reads from header/claim/subdomain/QS
- `TenantInfo` — resolved tenant record (TenantId, TenantName, Properties)
- `MultiTenancyOptions` — `Strategy`, `HeaderName`, `ClaimName`, `QueryStringKey`, `RequireTenant`
- `AddSharedMultiTenancy(IConfiguration)` — registers context, resolver, and options
- `UseSharedMultiTenancy()` — adds tenant middleware to the pipeline

## Rules

**Must:**
- Always register `UseSharedMultiTenancy()` before `UseAuthentication()`
- Use `ITenantContext` for all tenant-aware logic — never read headers directly
- Feature-flag or guard all multi-tenant code paths with `ITenantContext.IsResolved`
- Tenant IDs must be treated as untrusted user input (validate length, character set)

**Forbidden:**
- Sharing data across tenants without explicit cross-tenant permission
- Storing tenant context in static or singleton fields
- Feature flags or auth behavior controlled by tenant ID directly

## ⚠️ DATA ISOLATION — CRITICAL SECURITY REQUIREMENTS

**THIS PACKAGE DOES NOT ENFORCE DATA ISOLATION.** Each application layer MUST actively enforce boundaries:

### Database / Query Layer (🔴 HIGHEST PRIORITY)
- **MUST:** Every query includes `WHERE TenantId = @TenantId` using `ITenantContext.TenantId`
- **MUST:** Repository/EF Core DbContext has tenant filtering at query execution time (not schema-level)
- **MUST:** Validate TenantId length (max 255 chars) and character set (alphanumeric + `-_`) before queries
- **FORBIDDEN:** SELECT * without tenant filter, dynamic queries without parameterization, SQL injection vectors
- **Test:** Query tests must cover cross-tenant data access attempts and verify 404/403 results

### Cache Layer (🔴 HIGH PRIORITY)
- **MUST:** Cache keys include TenantId: `cache:tenant:{TenantId}:entity:{Id}`
- **MUST:** Cache invalidation respects tenant boundaries (invalidate only for that tenant)
- **MUST:** Distributed cache (Redis) keys are namespaced: `{app}:t-{TenantId}:{resource}`
- **FORBIDDEN:** Shared cache keys across tenants, global cache without tenant segments
- **Test:** Cache hit/miss tests verify no cross-tenant key collisions

### Authorization (🟡 MEDIUM PRIORITY)
- **MUST:** Verify user has role/permission **within the resolved tenant context**
- **MUST NOT:** Allow user from TenantA to access resources in TenantB even with valid credentials
- **Pattern:** `if (user.TenantId != context.TenantId) return Unauthorized();`
- **Test:** Auth tests verify cross-tenant request rejection

### Background Jobs / Async Work (🔴 HIGH PRIORITY)
- **FORBIDDEN:** Pass ITenantContext to background jobs (it's request-scoped)
- **MUST:** Capture TenantId as a **string value**, not a reference
- **Pattern:** `await _backgroundJobClient.EnqueueAsync<TenantTask>(t => t.ProcessAsync(tenantId, cancellationToken));`
- **Test:** Background job tests verify jobs process only their assigned tenant data

### Logging & Audit (🟡 MEDIUM PRIORITY)
- **MUST:** Include `TenantId` in correlation ID and log context (Serilog enricher)
- **MUST:** Audit logs include tenant identifier for forensics
- **FORBIDDEN:** Log full tenant data, user credentials, or PII
- **Test:** Audit trail tests verify tenant provenance for all operations

### Third-Party APIs & External Calls (🟡 MEDIUM PRIORITY)
- **MUST:** Validate response data belongs to current tenant before use
- **MUST:** Don't expose tenant-specific errors to other tenants (normalize error messages)
- **FORBIDDEN:** Leak tenant metadata in request/response headers to external APIs
- **Test:** API integration tests mock external calls and verify tenant boundaries

### Singleton Services (🔴 FORBIDDEN)
- **MUST NOT:** Store tenant-specific state in Singletons
- **Pattern:** Use Scoped services exclusively for tenant-aware logic
- Exception: Only inject `ITenantContext` into Scoped/Transient services, never Singletons
- **Test:** Architecture tests verify no Singleton has ITenantContext dependency

## Resolution Strategies

| Strategy | Source |
|----------|--------|
| `Header` | `X-Tenant-Id` request header (default) |
| `Claim` | JWT claim `tenant_id` |
| `Subdomain` | First label of the request host |
| `QueryString` | `?tenantId=...` query parameter |

## Design Decisions

`TenantContext` is `internal sealed` — consumers interact only via `ITenantContext`.
The middleware writes to `TenantContext`; application code reads from `ITenantContext`.
Custom resolvers replace the default by re-registering `ITenantResolver` after `AddSharedMultiTenancy`.

## Test Strategy

**Configuration & Records** (35 tests):
- `MultiTenancyOptionsTests` — defaults, IOptionsMonitor binding, validation
- `TenantInfoTests` — immutability, equality, property access
- `DefaultTenantResolverTests` — all 4 strategies with mocked `HttpContext`, null handling, edge cases

**Behavioral Tests** (20 tests):
- `TenantMiddlewareTests` — tenant resolution, context population, error handling (400 when required), next-middleware invocation
- `TenantContextBehaviorTests` — initialization, mutation, read-only interface contract, idempotent set operations
- `TenantContextIsolationTests` — scoped isolation (new instance per request scope), thread safety, no cross-request leakage
- `ServiceCollectionExtensionsTests` — DI registration (scoped context/resolver), options binding, startup validation
- `CustomTenantResolverTests` — custom resolver implementation patterns, database lookup simulation, strategy replacement

## Extension Points

- Implement `ITenantResolver` to resolve tenants from a database, cache, or external service
- Re-register after `AddSharedMultiTenancy` to replace the default resolver
