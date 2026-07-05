# Security Guidelines

See ADR-007 for the strategic decision. This document provides implementation detail.

## Secrets

### Development

```bash
dotnet user-secrets init
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"
```

Never commit `secrets.json`. It's gitignored by default.

### Production

Inject via environment variables or mount from secret manager:
```yaml
env:
  - name: Redis__ConnectionString
    valueFrom:
      secretKeyRef:
        name: redis-secret
        key: connection-string
```

## Input Validation

Use FluentValidation for all command/request objects:

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().ForEach(item => {
            item.ChildRules(i => i.RuleFor(x => x.Quantity).GreaterThan(0));
        });
    }
}
```

Register globally:
```csharp
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

## SQL Injection Prevention

Always use parameterized queries:

```csharp
// Good
await connection.QueryAsync<Order>(
    "SELECT * FROM Orders WHERE Id = @Id",
    new { Id = id });

// Bad — SQL injection risk
await connection.QueryAsync<Order>(
    $"SELECT * FROM Orders WHERE Id = '{id}'");
```

## Logging PII

```csharp
// Good
_logger.LogInformation("User {UserId} logged in", userId);

// Bad — logs email (PII)
_logger.LogInformation("User {Email} logged in", user.Email);

// Good — partial token for debugging
_logger.LogDebug("Using token ending in {TokenSuffix}", token[^4..]);
```

## Authorization

Always use policy-based auth, not role strings:

```csharp
[Authorize(Policy = "RequireOrderRead")]
public async Task<IActionResult> GetOrder(Guid id) { ... }

// Register:
services.AddAuthorization(options => {
    options.AddPolicy("RequireOrderRead", policy =>
        policy.RequireClaim("permission", "orders:read"));
});
```

## Rate Limiting

Apply rate limiting to all public endpoints:

```csharp
services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("api", o => {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromMinutes(1);
    });
});
```

## Multi-Tenancy Data Isolation

**CRITICAL:** SharedCommon.MultiTenancy provides tenant **identification only**, not isolation enforcement.
Application code MUST actively enforce boundaries at every layer.

### Query Layer (🔴 HIGHEST PRIORITY)

Every database query MUST filter by tenant:

```csharp
// Good — explicit tenant filtering
public async Task<Order?> GetOrderAsync(Guid orderId, CancellationToken ct)
{
    var tenantId = _tenantContext.TenantId;
    if (!IsValidTenantId(tenantId)) 
        return null;
        
    return await _db.Orders
        .Where(o => o.TenantId == tenantId && o.Id == orderId)
        .FirstOrDefaultAsync(ct);
}

// Bad — missing tenant filter (DATA LEAK)
public async Task<Order?> GetOrderAsync(Guid orderId, CancellationToken ct)
{
    return await _db.Orders
        .Where(o => o.Id == orderId)  // ❌ Any tenant can access any order
        .FirstOrDefaultAsync(ct);
}
```

**Rules:**
- Validate tenant ID format: `^[a-zA-Z0-9\-_]+$`, max 255 chars
- Use parameterized queries (prevents SQL injection)
- Apply tenant filter in WHERE clause at query execution time
- Unit tests MUST verify cross-tenant access attempts return 404/403

### Cache Layer (🔴 HIGH PRIORITY)

Cache keys MUST include tenant ID:

```csharp
// Good — tenant-scoped cache key
public async Task<Product?> GetProductAsync(Guid productId, CancellationToken ct)
{
    var cacheKey = $"tenant:{_tenantContext.TenantId}:product:{productId}";
    return await _cache.GetOrSetAsync(cacheKey, async () => 
        await _db.GetProductAsync(_tenantContext.TenantId, productId, ct), ct);
}

// Bad — shared cache key (TENANT COLLISION)
public async Task<Product?> GetProductAsync(Guid productId, CancellationToken ct)
{
    var cacheKey = $"product:{productId}";  // ❌ Tenants share cache entries
    return await _cache.GetAsync(cacheKey);
}
```

**Rules:**
- Distributed cache (Redis): `{app}:t-{TenantId}:{resource}`
- In-memory cache (HybridCache): `tenant:{TenantId}:entity:{id}`
- Invalidate only for the current tenant (not globally)
- Test cache hit/miss scenarios for cross-tenant collisions

### Background Jobs (🔴 HIGH PRIORITY)

Capture tenant ID as a **value**, never pass the context:

```csharp
// Good — capture tenant ID as string
public async Task ProcessOrderAsync(Guid orderId, CancellationToken ct)
{
    var tenantId = _tenantContext.TenantId;  // Capture as string value
    await _backgroundJobs.EnqueueAsync<OrderProcessor>(
        x => x.ProcessAsync(tenantId, orderId, ct));
}

// Bad — passing ITenantContext (SCOPED, will fail in background)
public async Task ProcessOrderAsync(Guid orderId, CancellationToken ct)
{
    await _backgroundJobs.EnqueueAsync<OrderProcessor>(
        x => x.ProcessAsync(_tenantContext, orderId, ct));  // ❌ Scoped context
}
```

**Rules:**
- ITenantContext is scoped per HTTP request; background jobs outlive the request
- Always capture tenant ID as `string`
- Verify background job code validates tenant ID before querying

### Authorization (🟡 MEDIUM PRIORITY)

Verify user has permission **within the resolved tenant**:

```csharp
// Good — cross-tenant check
public async Task<IActionResult> GetOrderAsync(Guid orderId)
{
    var order = await _service.GetOrderAsync(orderId);
    if (order is null) return NotFound();
    
    // Verify order belongs to resolved tenant (defense-in-depth)
    if (order.TenantId != _tenantContext.TenantId)
        return Unauthorized();
        
    // Verify user has permission in this tenant
    if (!User.IsInRole($"tenant:{_tenantContext.TenantId}:order:read"))
        return Forbid();
        
    return Ok(order);
}
```

### Logging & Audit (🟡 MEDIUM PRIORITY)

Include `TenantId` in correlation IDs and audit logs:

```csharp
// Register Serilog enricher
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("TenantId", () => _tenantContext?.TenantId ?? "system")
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .CreateLogger();

// Audit trail
_auditService.Log(new AuditEntry {
    TenantId = _tenantContext.TenantId,
    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
    Action = "Order.Retrieved",
    CorrelationId = HttpContext.TraceIdentifier,
    Timestamp = DateTime.UtcNow
});
```

**Rules:**
- Never log passwords, tokens, full card numbers, SSNs
- Include tenant identifier for forensics
- Use structured logging with `TenantId` as a property

### Third-Party APIs (🟡 MEDIUM PRIORITY)

Validate response data belongs to resolved tenant:

```csharp
public async Task<ShippingInfo> GetShippingAsync(Guid orderId, CancellationToken ct)
{
    var shipping = await _externalApi.GetShippingAsync(orderId, ct);
    
    // Validate response belongs to current tenant
    if (shipping.TenantId != _tenantContext.TenantId)
        throw new SecurityException($"Shipping data belongs to {shipping.TenantId}, not {_tenantContext.TenantId}");
        
    return shipping;
}
```

### Singleton Services (🔴 FORBIDDEN)

Never store tenant-specific data in Singletons:

```csharp
// Bad — singleton with tenant context
public sealed class OrderService : IOrderService  // Singleton
{
    private string? _cachedTenantId;  // ❌ THREAD UNSAFE
    
    public OrderService(ITenantContext tenantContext)  // ❌ SCOPED in SINGLETON
    {
        _cachedTenantId = tenantContext.TenantId;
    }
}

// Good — scoped service
public sealed class OrderService : IOrderService  // Scoped
{
    public OrderService(ITenantContext tenantContext)  // ✓ Scoped in Scoped
    {
        _tenantContext = tenantContext;
    }
}
```

**Rules:**
- Register tenant-aware services as `Scoped` (one per HTTP request)
- Never inject `ITenantContext` into `Singleton` services
- Use architecture tests to enforce this (see `SharedCommon.ArchitectureTests`)
