# SharedCommon.MultiTenancy

Request-scoped tenant resolution for multi-tenant ASP.NET Core services. Resolves a tenant from the incoming request (header, JWT claim, subdomain, or query string) and makes it available via `ITenantContext` throughout the request pipeline.

## ⚠️ CRITICAL SECURITY WARNING

**This package provides tenant IDENTIFICATION only.** Your application MUST enforce data isolation at all layers:

| Layer | Requirement |
|-------|-------------|
| **Queries** | Filter every query with `WHERE TenantId = @TenantId` |
| **Cache** | Include tenant ID in all cache keys |
| **Authorization** | Verify cross-tenant access and reject |
| **Background Jobs** | Capture tenant ID as `string`, not `ITenantContext` |
| **Logging** | Include `TenantId` in correlation IDs |
| **Third-Party APIs** | Validate response data belongs to resolved tenant |
| **Singletons** | FORBIDDEN to inject `ITenantContext` |

**See [Security Guidelines](../../docs/standards/security-guidelines.md#multi-tenancy-data-isolation) for implementation examples.**

---

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.MultiTenancy
```

## Registration

```csharp
// Services
builder.Services.AddSharedMultiTenancy(builder.Configuration);

// Middleware pipeline — place before UseAuthentication
app.UseSharedMultiTenancy();
app.UseAuthentication();
app.UseAuthorization();
```

## Configuration

```json
{
  "SharedCommon": {
    "MultiTenancy": {
      "Enabled": true,
      "Strategy": "Header",
      "HeaderName": "X-Tenant-Id",
      "ClaimName": "tenant_id",
      "QueryStringKey": "tenantId",
      "RequireTenant": false
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `Enabled` | `true` | Set to `false` to bypass resolution globally. |
| `Strategy` | `Header` | `Header`, `Claim`, `Subdomain`, or `QueryString`. |
| `HeaderName` | `X-Tenant-Id` | Header name used by the `Header` strategy. |
| `ClaimName` | `tenant_id` | JWT claim name used by the `Claim` strategy. |
| `QueryStringKey` | `tenantId` | Query string key used by the `QueryString` strategy. |
| `RequireTenant` | `false` | When `true`, requests without a resolved tenant receive HTTP 400. |

---

## Usage

Inject `ITenantContext` wherever tenant-aware logic is needed:

```csharp
public class OrderService(ITenantContext tenant, IOrderRepository repo)
{
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct)
    {
        if (!tenant.IsResolved)
            return [];

        return await repo.GetByTenantAsync(tenant.TenantId, ct);
    }
}
```

Always guard with `IsResolved` unless `RequireTenant: true` guarantees a tenant is present.

---

## Resolution Strategies

| Strategy | Source | Example |
|----------|--------|---------|
| `Header` | Request header | `X-Tenant-Id: acme` |
| `Claim` | JWT claim | `"tenant_id": "acme"` in token payload |
| `Subdomain` | First host label | `acme.api.example.com` → `acme` |
| `QueryString` | Query parameter | `?tenantId=acme` |

---

## Custom Resolver

Implement `ITenantResolver` to resolve tenants from a database, cache, or external service:

```csharp
public class DatabaseTenantResolver(ITenantRepository repo) : ITenantResolver
{
    public async Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var id))
            return null;

        var tenant = await repo.FindBySlugAsync(id.ToString(), ct);
        return tenant is null ? null : new TenantInfo(tenant.Id, tenant.Name);
    }
}
```

Register it after `AddSharedMultiTenancy` to replace the default resolver:

```csharp
builder.Services.AddSharedMultiTenancy(builder.Configuration);
builder.Services.AddScoped<ITenantResolver, DatabaseTenantResolver>(); // overrides default
```

---

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `ITenantContext` | Scoped | Read-only view of the resolved tenant. |
| `ITenantResolver` | Scoped | `DefaultTenantResolver` by default; replace to customise resolution. |
| `MultiTenancyOptions` | Singleton (Options) | Validated at startup. |
