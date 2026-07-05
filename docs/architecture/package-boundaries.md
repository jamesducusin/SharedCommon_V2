# Package Boundaries

## What Goes Where

### SharedCommon.Core
**Contains:**
- `Result<T>` and `Error` types
- Base interfaces: `IEntity`, `IValueObject`, `IAggregateRoot`
- Common exceptions: `DomainException`, `ValidationException`
- `PagedResult<T>`, `Pagination`
- Utility extensions that have no external dependencies

**Does not contain:**
- Logging infrastructure
- DI registrations (beyond its own)
- Any I/O or network operations

---

### SharedCommon.Logging
**Contains:**
- `IStructuredLogger` abstraction
- Serilog configuration builders
- Enrichers: `CorrelationIdEnricher`, `EnvironmentEnricher`
- `LogContext` helpers
- Sink configuration extensions (ElasticSearch, File, Console)

**Does not contain:**
- Middleware (belongs in SharedCommon.Middlewares)
- Business-domain log events

---

### SharedCommon.Caching
**Contains:**
- `ICacheService` abstraction
- `DistributedCacheService` (Redis)
- `InMemoryCacheService`
- `HybridCacheService` (L1 + L2)
- `CacheOptions`
- Cache key builders

**Does not contain:**
- Auth-aware caching (keep auth out of cache layer)
- Domain-specific cache keys

---

### SharedCommon.Auth
**Contains:**
- JWT validation configuration
- `ICurrentUser` abstraction and accessor
- `ITokenService` for issuing tokens
- Policy-based authorization extensions
- API key validation

**Does not contain:**
- User management (belongs in consuming service)
- Password hashing (belongs in SharedCommon.Security)

---

### SharedCommon.Middlewares
**Contains:**
- `CorrelationIdMiddleware`
- `RequestLoggingMiddleware`
- `ExceptionHandlingMiddleware`
- `ResponseTimeMiddleware`
- Registration extension methods

**Does not contain:**
- Business logic
- Database calls
- Auth decisions (delegate to Auth package)

---

### SharedCommon.ResponseBuilder
**Contains:**
- `ApiResponse<T>` envelope
- `ProblemDetails` extensions
- Standard error response builders
- `IResponseBuilder` abstraction

**Does not contain:**
- HTTP-specific concerns beyond response shaping
- Logging (done before the response is built)

---

### SharedCommon.MultiTenancy
**Contains:**
- `ITenantContext` — request-scoped tenant identity
- `ITenantResolver` — tenant resolution strategies (header, claim, subdomain, query string)
- `TenantMiddleware` — request pipeline integration
- `TenantInfo` — resolved tenant record
- `MultiTenancyOptions` — strategy and behavior configuration
- Tenant resolution registration extension

**Does not contain:**
- Data isolation enforcement (application responsibility)
- Tenant storage/persistence (belongs in consuming service)
- Tenant provisioning (belongs in consuming service)

**CRITICAL SECURITY:**
This package provides tenant **identification only**. Application code MUST enforce isolation at:
- **Query Layer:** WHERE clauses filtering by tenant ID
- **Cache Layer:** Tenant ID in cache keys
- **Authorization:** Cross-tenant access verification
- **Background Jobs:** Capture tenant ID as string value (context is scoped)
- **Logging:** Include TenantId in audit trails and correlation IDs
- **Third-Party APIs:** Validate response data belongs to resolved tenant
- **Singletons:** FORBIDDEN to inject ITenantContext

See [Security Guidelines](../standards/security-guidelines.md#multi-tenancy-data-isolation) for implementation patterns.
