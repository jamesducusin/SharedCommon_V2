# Caching Skill

Design and implement Redis, in-memory, and hybrid caching.

## When to Use This Skill

Triggers:
- Adding caching to a service
- Choosing between in-memory and distributed cache
- Setting TTL and eviction policies
- Preventing cache stampede

Ask Claude explicitly: "Use caching skill"

## Input (What You Provide)

- Data to cache (type, size, read/write ratio)
- Freshness requirements
- Distribution requirements (single vs multi-instance)

## Output (What You Get)

- Cache strategy recommendation
- Implementation with correct TTL and key design
- Stampede protection if needed

## Decision Tree

```
Is data shared across multiple service instances?
  Yes → Distributed cache (Redis)
  No  → In-memory cache (IMemoryCache)

Is data extremely hot (>1000 req/s)?
  Yes → Consider L1 in-memory + L2 Redis (hybrid)
  No  → Single layer sufficient

Can data be stale for seconds?
  Yes → Cache-aside pattern
  No  → Cache invalidation on write required
```

## Checklist

- [ ] Cache key includes all discriminating factors (user, tenant, params)
- [ ] TTL set appropriately for data freshness
- [ ] SemaphoreSlim used for stampede protection on cold start
- [ ] Null values handled (cache negative results to prevent DB hammering)
- [ ] Cache size limits configured for IMemoryCache
- [ ] Redis connection resilient (retry policy, circuit breaker)
- [ ] Serialization consistent (System.Text.Json preferred)
- [ ] Cache metrics instrumented (hit rate, miss rate)

## Cache Key Convention

```
{package}:{entity}:{id}[:{discriminator}]

Examples:
orders:order:12345
catalog:product:sku-abc:tenant-42
auth:permissions:user-99
```

## Stampede Protection Pattern

```csharp
private readonly SemaphoreSlim _lock = new(1, 1);

public async Task<Order?> GetOrderAsync(Guid id, CancellationToken ct)
{
    var key = $"orders:order:{id}";
    if (_cache.TryGetValue(key, out Order? cached)) return cached;

    await _lock.WaitAsync(ct);
    try
    {
        if (_cache.TryGetValue(key, out cached)) return cached;
        var order = await _repository.GetAsync(id, ct);
        _cache.Set(key, order, TimeSpan.FromMinutes(5));
        return order;
    }
    finally { _lock.Release(); }
}
```

## References

See: docs/adr/ADR-003-hybrid-cache-strategy.md
See: src/SharedCommon.Caching/CLAUDE.md
See: docs/standards/performance-guidelines.md
