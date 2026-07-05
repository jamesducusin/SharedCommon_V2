# SharedCommon.Caching

Hybrid caching abstraction that hides cache topology behind a single `ICacheService` interface. Supports in-memory (L1), Redis (L2), and database (L3) tiers — configured via `appsettings.json` with no code changes.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Caching
```

## Registration

```csharp
builder.Services.AddSharedCaching(builder.Configuration);
```

## Configuration

### Memory-only (simplest, no Redis required)

```json
{
  "SharedCommon": {
    "Caching": {
      "DefaultProvider": "Memory",
      "DefaultTtlSeconds": 300
    }
  }
}
```

### Hybrid (memory L1 + Redis L2, recommended for production)

```json
{
  "SharedCommon": {
    "Caching": {
      "DefaultProvider": "Hybrid",
      "DefaultTtlSeconds": 300,
      "Redis": {
        "Enabled": true,
        "Connection": "redis:6379",
        "KeyPrefix": "orders:",
        "Ssl": false
      },
      "Hybrid": {
        "L1Enabled": true,
        "L2Enabled": true,
        "PromoteOnHit": true,
        "InvalidateDownstream": true
      }
    }
  }
}
```

### Key properties

| Property | Default | Notes |
|----------|---------|-------|
| `DefaultProvider` | `Hybrid` | `Memory` \| `Redis` \| `Hybrid` |
| `DefaultTtlSeconds` | `300` | Used when no expiration is passed to `SetAsync`. |
| `Redis.Enabled` | `false` | Must be `true` for Redis/Hybrid providers. |
| `Redis.Connection` | — | StackExchange.Redis connection string. Use secrets. |
| `Redis.KeyPrefix` | `sharedcommon:` | Prepended to every Redis key. |
| `Memory.MaximumSize` | `10000` | LRU eviction triggers above this count. |

## Usage

### Inject ICacheService

```csharp
public class ProductService(ICacheService cache)
{
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        // GetOrSetAsync — stampede-safe get-or-populate
        return await cache.GetOrSetAsync(
            key: $"products:{id}",
            factory: async c => await _repo.GetByIdAsync(id, c),
            expiration: TimeSpan.FromMinutes(10),
            ct: ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct)
    {
        await _repo.UpdateAsync(product, ct);
        // Invalidate on write
        await cache.RemoveAsync($"products:{product.Id}", ct);
    }
}
```

### Cache key convention

Use `{namespace}:{entity}:{id}` to avoid collisions across services:

```
products:detail:42
orders:summary:user:7
sessions:token:abc123
```

### Batch operations

```csharp
// Write multiple entries at once
var items = new Dictionary<string, Product>
{
    ["products:1"] = product1,
    ["products:2"] = product2,
};
await cache.SetManyAsync(items, TimeSpan.FromMinutes(5), ct);

// Read multiple keys
var found = await cache.GetManyAsync<Product>(["products:1", "products:2"], ct);
```

### Pattern invalidation (Redis only)

```csharp
// Removes all keys matching the glob pattern
await cache.InvalidateByPatternAsync("orders:user:42:*", ct);
```

### Statistics

```csharp
var stats = await cache.GetStatisticsAsync(ct);
Console.WriteLine($"Hit rate: {stats.HitRate:P0}");
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `ICacheService` | Singleton | Concrete type depends on `DefaultProvider`. |
| `IMemoryCache` | Singleton | Always registered. |
| `IDistributedCache` | Singleton | Redis-backed when `Redis.Enabled` is true. |
