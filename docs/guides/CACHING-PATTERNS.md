# Caching Patterns & Strategies

## Overview

Effective caching is critical for performance, scalability, and cost efficiency. This guide covers distributed caching patterns, Redis strategies, cache invalidation, and telemetry integration for the SharedCommon platform.

## Table of Contents

1. [Caching Fundamentals](#caching-fundamentals)
2. [Cache-Aside Pattern](#cache-aside-pattern)
3. [Write-Through Pattern](#write-through-pattern)
4. [Write-Behind Pattern](#write-behind-pattern)
5. [Redis Configuration](#redis-configuration)
6. [TTL & Expiration](#ttl--expiration)
7. [Cache Invalidation](#cache-invalidation)
8. [Distributed Caching](#distributed-caching)
9. [Telemetry & Monitoring](#telemetry--monitoring)
10. [Troubleshooting](#troubleshooting)

---

## Caching Fundamentals

### Cache Levels

| Level | Scope | Latency | Size | Use Case |
|-------|-------|---------|------|----------|
| **L1: In-Memory** | Single process | <1ms | MB | Frequently accessed immutable data |
| **L2: Distributed** | All services (Redis) | 1-10ms | GB | Shared state, sessions, user data |
| **L3: CDN** | Global edge locations | 10-50ms | TB | Static assets, API responses |
| **L4: Database** | Persistent storage | 100-1000ms | ∞ | Source of truth |

### Caching Strategy Selection

```
Request → L1 (in-memory) → L2 (Redis) → L3 (CDN) → L4 (Database)
                    ↓ miss
         Check all levels → compute → populate lower levels
```

**Decision Tree:**

```
Is data frequently read?
├─ Yes
│  ├─ Is it mutable?
│  │  ├─ Rarely: use cache-aside with long TTL
│  │  └─ Often: use write-through for consistency
│  └─ No: use in-memory cache with app startup
└─ No: don't cache, hit database directly
```

---

## Cache-Aside Pattern

### Overview

Application checks cache first; on miss, fetch from database and populate cache.

**When to use:** Read-heavy workloads, eventual consistency acceptable.

### Implementation

```csharp
public async Task<OrderDto> GetOrderByIdAsync(Guid orderId, CancellationToken ct)
{
    var cacheKey = $"order:{orderId}";

    // Step 1: Try cache
    var cached = await _cache.GetAsync<OrderDto>(cacheKey, ct);
    if (cached != null)
    {
        _telemetry.RecordMetric("cache.hit", 1, new() { { "key", cacheKey } });
        return cached;
    }

    // Step 2: Cache miss - fetch from database
    _telemetry.RecordMetric("cache.miss", 1, new() { { "key", cacheKey } });
    var order = await _database.GetOrderByIdAsync(orderId, ct);

    // Step 3: Populate cache
    if (order != null)
    {
        await _cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(60), ct);
    }

    return order;
}
```

### Pros & Cons

| Pros | Cons |
|------|------|
| Simple to implement | Cache stampede on expiry |
| Works well for read-heavy loads | Stale data possible |
| Easy to add/remove | Cold cache on startup |

### Cache Stampede Mitigation

```csharp
private readonly SemaphoreSlim _cacheStampedeMutex = new(1);

public async Task<OrderDto> GetOrderByIdWithStampedePreventionAsync(
    Guid orderId,
    CancellationToken ct)
{
    var cacheKey = $"order:{orderId}";
    var cached = await _cache.GetAsync<OrderDto>(cacheKey, ct);
    if (cached != null) return cached;

    // Lock during cache population
    if (!await _cacheStampedeMutex.WaitAsync(TimeSpan.FromSeconds(5), ct))
    {
        // Timeout - fetch from DB directly
        return await _database.GetOrderByIdAsync(orderId, ct);
    }

    try
    {
        // Double-check after lock acquired
        cached = await _cache.GetAsync<OrderDto>(cacheKey, ct);
        if (cached != null) return cached;

        var order = await _database.GetOrderByIdAsync(orderId, ct);
        if (order != null)
        {
            await _cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(60), ct);
        }
        return order;
    }
    finally
    {
        _cacheStampedeMutex.Release();
    }
}
```

---

## Write-Through Pattern

### Overview

Application writes to cache and database synchronously. Guarantees consistency.

**When to use:** Write-heavy workloads, consistency critical (financial transactions).

### Implementation

```csharp
public async Task<Guid> CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
{
    using var scope = _telemetry.StartOperation("CreateOrder", "mutation");

    try
    {
        // Step 1: Validate
        var order = new Order { /* ... */ };

        // Step 2: Write to database
        var orderId = await _database.CreateOrderAsync(order, ct);
        scope.SetTag("database.write.ok", true);

        // Step 3: Write to cache
        var cacheKey = $"order:{orderId}";
        await _cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(60), ct);
        scope.SetTag("cache.write.ok", true);

        _telemetry.RecordMetric("order.created", 1, new() { { "source", "write_through" } });
        scope.MarkSucceeded();
        return orderId;
    }
    catch (Exception ex)
    {
        scope.RecordException(ex);
        throw;
    }
}
```

### Pros & Cons

| Pros | Cons |
|------|------|
| Strong consistency | Slower writes (2 operations) |
| No stale data | Cache failures block writes |
| Simple mental model | Requires consistent cache |

### Failure Handling

```csharp
public async Task<Guid> CreateOrderWithFallbackAsync(
    CreateOrderCommand command,
    CancellationToken ct)
{
    var orderId = await _database.CreateOrderAsync(command.ToEntity(), ct);
    var cacheKey = $"order:{orderId}";

    try
    {
        await _cache.SetAsync(cacheKey, command, TimeSpan.FromMinutes(60), ct);
    }
    catch (Exception ex)
    {
        // Cache write failure - log but don't fail operation
        _logger.LogWarning(ex, "Failed to cache order {OrderId}", orderId);
        _telemetry.RecordMetric("cache.write.failed", 1);
        
        // Queue background job to retry cache population
        await _backgroundJobs.EnqueueAsync(() => 
            RepopulateCacheAsync(orderId, ct));
    }

    return orderId;
}

private async Task RepopulateCacheAsync(Guid orderId, CancellationToken ct)
{
    var order = await _database.GetOrderByIdAsync(orderId, ct);
    if (order != null)
    {
        var cacheKey = $"order:{orderId}";
        await _cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(60), ct);
    }
}
```

---

## Write-Behind Pattern

### Overview

Application writes to cache immediately, database write happens asynchronously.

**When to use:** High-throughput scenarios, eventual consistency acceptable.

**Trade-off:** High performance, risk of data loss if cache crashes before flush.

### Implementation

```csharp
private readonly Channel<CacheWriteJob> _writeQueue = Channel.CreateUnbounded<CacheWriteJob>();

public async Task<Guid> CreateOrderWithWriteBehindAsync(
    CreateOrderCommand command,
    CancellationToken ct)
{
    var orderId = Guid.NewGuid();
    var cacheKey = $"order:{orderId}";
    var order = command.ToEntity(orderId);

    // Step 1: Write to cache immediately
    await _cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(60), ct);
    _telemetry.RecordMetric("cache.write", 1, new() { { "pattern", "write_behind" } });

    // Step 2: Queue database write
    await _writeQueue.Writer.WriteAsync(
        new CacheWriteJob { OrderId = orderId, Order = order },
        ct);

    return orderId;
}

// Background worker processes queue
public async Task ProcessWriteBehindQueueAsync(CancellationToken ct)
{
    await foreach (var job in _writeQueue.Reader.ReadAllAsync(ct))
    {
        try
        {
            await _database.CreateOrderAsync(job.Order, ct);
            _telemetry.RecordMetric("database.write_behind", 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Write-behind failed for order {OrderId}", job.OrderId);
            _telemetry.RecordMetric("database.write_behind_failed", 1);
            
            // Retry logic with exponential backoff
            await _retryPolicy.ExecuteAsync(() =>
                _database.CreateOrderAsync(job.Order, ct));
        }
    }
}

private record CacheWriteJob(Guid OrderId, Order Order);
```

---

## Redis Configuration

### Connection Pooling

```csharp
public static IServiceCollection AddDistributedCaching(
    this IServiceCollection services,
    IConfiguration config)
{
    var redisConfig = ConfigurationOptions.Parse(config["Redis:ConnectionString"]);
    
    // Connection pooling
    redisConfig.ConnectTimeout = 5000;
    redisConfig.ConnectRetry = 3;
    redisConfig.SyncTimeout = 5000;
    redisConfig.KeepAlive = 60;
    
    // Performance
    redisConfig.AllowAdmin = false;
    redisConfig.CommandMap = CommandMap.Filter; // Restrict dangerous commands

    var connection = ConnectionMultiplexer.Connect(redisConfig);
    
    services.AddSingleton(connection);
    services.AddStackExchangeRedisCache(options =>
    {
        options.ConnectionMultiplexerFactory = () => connection;
        options.InstanceName = "templates:";
    });

    return services;
}
```

### Configuration by Environment

```yaml
# Development
Redis:
  ConnectionString: "localhost:6379"
  DefaultExpiry: 3600  # 1 hour
  MaxConnections: 10

# Staging
Redis:
  ConnectionString: "redis-staging.internal:6379"
  DefaultExpiry: 1800  # 30 minutes
  MaxConnections: 50

# Production
Redis:
  ConnectionString: "redis-prod-cluster.internal:6379"
  DefaultExpiry: 900   # 15 minutes
  MaxConnections: 200
  HighAvailability: true
  Sentinel:
    - "sentinel-1:26379"
    - "sentinel-2:26379"
```

### StackExchange.Redis Best Practices

```csharp
public class RedisCacheService : IDistributedCache
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ITelemetryService _telemetry;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("CacheGet", "cache");
        scope.SetTag("cache.key", key);

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var db = _connection.GetDatabase();
            var value = await db.StringGetAsync(key);
            sw.Stop();

            scope.SetTag("cache.duration_ms", sw.ElapsedMilliseconds);
            scope.SetTag("cache.hit", value.HasValue);

            if (!value.HasValue)
            {
                _telemetry.RecordMetric("cache.miss", 1);
                return default;
            }

            var result = JsonSerializer.Deserialize<T>(value.ToString());
            _telemetry.RecordMetric("cache.hit", 1);
            scope.MarkSucceeded();

            return result;
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("cache.error", 1);
            throw;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry,
        CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("CacheSet", "cache");
        scope.SetTag("cache.key", key);
        scope.SetTag("cache.ttl_seconds", expiry?.TotalSeconds ?? 0);

        try
        {
            var db = _connection.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await db.StringSetAsync(key, json, expiry);

            sw.Stop();
            scope.SetTag("cache.duration_ms", sw.ElapsedMilliseconds);
            scope.MarkSucceeded();
            _telemetry.RecordMetric("cache.set", 1);
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _telemetry.RecordMetric("cache.set_error", 1);
            throw;
        }
    }
}
```

---

## TTL & Expiration

### Per-Data-Type TTL Strategy

| Data Type | TTL | Reason | Invalidation Trigger |
|-----------|-----|--------|----------------------|
| User profile | 1 hour | Identity data, moderate change rate | User profile update |
| Product catalog | 24 hours | Stable, batch updates | Scheduled refresh, webhook |
| Shopping cart | 7 days | Session-like, user-driven expiry | Checkout, manual clear |
| Order status | 30 days | Historical, immutable after complete | Order lifecycle event |
| Analytics/Metrics | 5 minutes | Real-time, high volume | Query result |
| Temporary tokens | 15 minutes | Security, frequent refresh | Token expiry |

### TTL Configuration

```csharp
public class CacheTtlConfiguration
{
    public static readonly Dictionary<string, TimeSpan> Defaults = new()
    {
        ["user:profile"] = TimeSpan.FromHours(1),
        ["product:details"] = TimeSpan.FromHours(24),
        ["order:status"] = TimeSpan.FromDays(30),
        ["cart:items"] = TimeSpan.FromDays(7),
        ["token:refresh"] = TimeSpan.FromMinutes(15),
        ["analytics:session"] = TimeSpan.FromMinutes(5),
    };

    public static TimeSpan GetTtl(string keyPattern)
    {
        return Defaults.TryGetValue(keyPattern, out var ttl) 
            ? ttl 
            : TimeSpan.FromHours(1); // Default fallback
    }
}

// Usage
var ttl = CacheTtlConfiguration.GetTtl("user:profile");
await _cache.SetAsync("user:123", user, ttl);
```

### Sliding Expiration

```csharp
public async Task<UserSession> GetOrCreateSessionAsync(
    Guid userId,
    CancellationToken ct)
{
    var cacheKey = $"session:{userId}";
    var session = await _cache.GetAsync<UserSession>(cacheKey, ct);

    if (session != null)
    {
        // Extend expiry on access (sliding window)
        await _cache.SetAsync(cacheKey, session, TimeSpan.FromMinutes(30), ct);
        return session;
    }

    // Create new session
    session = new UserSession { UserId = userId, CreatedAt = DateTime.UtcNow };
    await _cache.SetAsync(cacheKey, session, TimeSpan.FromMinutes(30), ct);

    return session;
}
```

---

## Cache Invalidation

### Strategy Selection

| Strategy | Consistency | Complexity | Use Case |
|----------|-------------|-----------|----------|
| **Time-based** | Eventual | Low | Low-urgency data |
| **Event-based** | Strong | Medium | Transactional data |
| **Manual** | Strong | High | Admin operations |
| **Broadcast** | Strong | High | Multi-service systems |

### Time-Based Invalidation

```csharp
// TTL handles expiry automatically
await _cache.SetAsync("product:123", product, TimeSpan.FromHours(1));
// Expires automatically after 1 hour
```

### Event-Based Invalidation

```csharp
public async Task UpdateOrderStatusAsync(Guid orderId, string newStatus)
{
    // Update database
    await _database.UpdateOrderStatusAsync(orderId, newStatus);

    // Invalidate cache entry
    var cacheKey = $"order:{orderId}";
    await _cache.RemoveAsync(cacheKey);

    // Publish event for other services
    await _eventBus.PublishAsync(new OrderStatusChangedEvent
    {
        OrderId = orderId,
        NewStatus = newStatus
    });

    _telemetry.RecordMetric("cache.invalidated", 1, new() { { "reason", "event" } });
}

// Event subscriber in another service
public async Task OnOrderStatusChangedAsync(OrderStatusChangedEvent @event)
{
    var localCacheKey = $"order:@event.OrderId";
    await _cache.RemoveAsync(localCacheKey);
}
```

### Pattern-Based Invalidation

```csharp
public async Task InvalidateCategoryProductsAsync(Guid categoryId)
{
    // Invalidate all products in category
    var pattern = $"product:category:{categoryId}:*";
    
    await _cache.RemoveByPatternAsync(pattern);

    _telemetry.RecordMetric("cache.pattern_invalidation", 1, new()
    {
        { "pattern", pattern }
    });
}

// Redis implementation
public async Task RemoveByPatternAsync(string pattern)
{
    var db = _connection.GetDatabase();
    var server = _connection.GetServer(_connection.GetEndPoints().First());

    var keys = server.Keys(pattern: pattern).ToList();
    if (keys.Count > 0)
    {
        await db.KeyDeleteAsync(keys.ToArray());
        _telemetry.RecordMetric("cache.keys_deleted", keys.Count);
    }
}
```

### Cascade Invalidation

```csharp
public async Task InvalidateUserDataAsync(Guid userId)
{
    var keysToInvalidate = new[]
    {
        $"user:profile:{userId}",
        $"user:preferences:{userId}",
        $"user:orders:{userId}",
        $"user:cart:{userId}",
        $"user:wishlist:{userId}",
    };

    using var scope = _telemetry.StartOperation("CascadeInvalidation", "cache");
    scope.SetTag("user.id", userId);
    scope.SetTag("keys.count", keysToInvalidate.Length);

    var tasks = keysToInvalidate.Select(key => _cache.RemoveAsync(key));
    await Task.WhenAll(tasks);

    scope.MarkSucceeded();
    _telemetry.RecordMetric("cache.cascade_invalidation", keysToInvalidate.Length);
}
```

---

## Distributed Caching

### Cache-Aside with Distributed Locks

```csharp
public async Task<Product> GetProductWithDistributedLockAsync(
    Guid productId,
    CancellationToken ct)
{
    var cacheKey = $"product:{productId}";
    var lockKey = $"{cacheKey}:lock";

    // Try cache first
    var cached = await _cache.GetAsync<Product>(cacheKey, ct);
    if (cached != null) return cached;

    // Acquire lock to prevent stampede
    using var lockHandle = await _distributedLock.AcquireAsync(
        lockKey,
        TimeSpan.FromSeconds(5),
        ct);

    if (lockHandle == null)
    {
        // Couldn't acquire lock - wait and retry cache
        await Task.Delay(100, ct);
        return await _cache.GetAsync<Product>(cacheKey, ct) 
            ?? throw new InvalidOperationException("Cache population timeout");
    }

    try
    {
        // Double-check cache after lock
        var rechecked = await _cache.GetAsync<Product>(cacheKey, ct);
        if (rechecked != null) return rechecked;

        // Fetch and cache
        var product = await _database.GetProductAsync(productId, ct);
        if (product != null)
        {
            await _cache.SetAsync(cacheKey, product, TimeSpan.FromHours(1), ct);
        }
        return product;
    }
    finally
    {
        await lockHandle.ReleaseAsync();
    }
}
```

### Warm Cache on Startup

```csharp
public class CacheWarmupService : IHostedService
{
    private readonly IDistributedCache _cache;
    private readonly IProductService _products;

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _telemetry.StartOperation("WarmCache", "startup");

        try
        {
            // Pre-load critical data
            var topProducts = await _products.GetTopProductsAsync(1000, ct);
            var tasks = topProducts.Select(p =>
                _cache.SetAsync(
                    $"product:{p.Id}",
                    p,
                    TimeSpan.FromHours(1),
                    ct));

            await Task.WhenAll(tasks);

            scope.SetTag("items_cached", topProducts.Count);
            scope.MarkSucceeded();
            _telemetry.RecordMetric("cache.warmup.success", topProducts.Count);
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            _logger.LogWarning(ex, "Cache warmup failed - continuing with cold cache");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

---

## Telemetry & Monitoring

### Key Metrics

```csharp
public interface ICacheMetrics
{
    void RecordHit(string key, long latencyMs);
    void RecordMiss(string key);
    void RecordError(string operation, Exception ex);
    void RecordEviction(int count);
}

public class CacheMetricsCollector : ICacheMetrics
{
    private readonly ITelemetryService _telemetry;

    public void RecordHit(string key, long latencyMs)
    {
        _telemetry.RecordMetric("cache.hits", 1, new()
        {
            { "key_pattern", ExtractPattern(key) }
        });
        
        _telemetry.RecordMetric("cache.hit_latency_ms", (double)latencyMs);
    }

    public void RecordMiss(string key)
    {
        _telemetry.RecordMetric("cache.misses", 1, new()
        {
            { "key_pattern", ExtractPattern(key) }
        });
    }

    public void RecordError(string operation, Exception ex)
    {
        _telemetry.RecordMetric("cache.errors", 1, new()
        {
            { "operation", operation },
            { "error_type", ex.GetType().Name }
        });
    }

    public void RecordEviction(int count)
    {
        _telemetry.RecordMetric("cache.evictions", count);
    }

    private static string ExtractPattern(string key)
    {
        // Extract pattern for metrics (user:* instead of user:12345)
        var colon = key.IndexOf(':');
        return colon > 0 ? key[..colon] + ":*" : "unknown";
    }
}
```

### Prometheus Queries

```promql
# Cache hit rate
rate(cache_hits[5m]) / (rate(cache_hits[5m]) + rate(cache_misses[5m]))

# P99 cache latency
histogram_quantile(0.99, rate(cache_hit_latency_ms_bucket[5m]))

# Cache error rate
rate(cache_errors[5m])

# Cache eviction rate
rate(cache_evictions[5m])
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "Cache Performance",
    "panels": [
      {
        "title": "Hit Rate %",
        "targets": [
          {
            "expr": "rate(cache_hits[5m]) / (rate(cache_hits[5m]) + rate(cache_misses[5m])) * 100"
          }
        ],
        "thresholds": [80, 90]
      },
      {
        "title": "P99 Latency (ms)",
        "targets": [
          {
            "expr": "histogram_quantile(0.99, rate(cache_hit_latency_ms_bucket[5m]))"
          }
        ],
        "alert": "P99 > 100ms"
      }
    ]
  }
}
```

---

## Troubleshooting

### High Miss Rate

**Symptoms:** Low hit rate, frequent database hits

**Investigation:**
```csharp
// Check TTL configuration
var ttl = CacheTtlConfiguration.GetTtl("user:profile"); // Should be 1h
if (ttl < TimeSpan.FromMinutes(5)) 
    // TTL too short - increase it
```

**Solutions:**
- Increase TTL for stable data
- Pre-warm cache on startup
- Check for unintended invalidation

### Cache Stampede

**Symptoms:** Spike in database load after cache expiry

**Solution:** Use distributed lock (see section above)

### Memory Growth

**Symptoms:** Redis memory usage growing unbounded

**Investigation:**
```bash
# Redis CLI
redis-cli INFO memory
redis-cli KEYS * | wc -l
```

**Solutions:**
- Implement TTL on all cache entries
- Use eviction policy: `maxmemory-policy allkeys-lru`
- Reduce cache retention time
- Monitor with `cache.evictions` metric

### Network Latency

**Symptoms:** High cache operation latency despite simple operations

**Solutions:**
- Enable TCP keepalive
- Batch operations with pipelining
- Use local L1 cache for hot data
- Check network connectivity to Redis

---

## Best Practices

1. **Always set TTL** - Prevents unbounded memory growth
2. **Use cache-aside for reads** - Simple, safe, supports partial failures
3. **Use write-through for consistency** - Financial transactions, critical updates
4. **Monitor hit rates** - Target 80%+ for effective caching
5. **Instrument with telemetry** - Track latency, errors, and patterns
6. **Test failure scenarios** - Redis crash, connection loss, timeout
7. **Use key namespacing** - `service:feature:entity:id`
8. **Implement backpressure** - Don't queue unlimited cache writes
9. **Plan capacity** - Redis memory = max concurrent sessions × average data size
10. **Document TTL strategy** - Why each key has its specific expiry

