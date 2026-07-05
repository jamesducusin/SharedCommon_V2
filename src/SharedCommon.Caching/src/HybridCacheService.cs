using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SharedCommon.Caching;

/// <summary>
/// <see cref="ICacheService"/> implementation that orchestrates L1 (in-memory) and
/// L2 (Redis/distributed) tiers.
///
/// Read path: L1 → L2 → miss.
/// Write path: write all enabled tiers.
/// On L2 hit with <c>PromoteOnHit = true</c>: populates L1 to avoid repeated network hops.
/// On L2 unavailability: logs a warning and continues with L1 only (graceful degradation).
///
/// Used when <c>DefaultProvider = Hybrid</c>.
/// </summary>
public sealed class HybridCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _l1;
    private readonly IDistributedCache? _l2;
    private readonly CachingOptions _options;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, byte> _l1Keys = new();

    private long _hits;
    private long _misses;
    private DateTimeOffset _lastCleared = DateTimeOffset.UtcNow;

    /// <summary>Initializes the hybrid cache service.</summary>
    /// <param name="l1">In-memory cache (always required).</param>
    /// <param name="l2">Distributed cache (Redis). May be <c>null</c> when Redis is disabled.</param>
    /// <param name="options">Caching configuration.</param>
    /// <param name="logger">Logger.</param>
    public HybridCacheService(
        IMemoryCache l1,
        IDistributedCache? l2,
        IOptions<CachingOptions> options,
        ILogger<HybridCacheService> logger)
    {
        ArgumentNullException.ThrowIfNull(l1);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _l1 = l1;
        _l2 = l2;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        ValidateKey(key);

        // L1
        if (_options.Hybrid.L1Enabled && _l1.TryGetValue(key, out string? l1Serialized) && l1Serialized is not null)
        {
            Interlocked.Increment(ref _hits);
            return JsonSerializer.Deserialize<T>(l1Serialized);
        }

        // L2
        if (_options.Hybrid.L2Enabled && _l2 is not null)
        {
            try
            {
                var l2Bytes = await _l2.GetAsync(PrefixKey(key), ct).ConfigureAwait(false);
                if (l2Bytes is not null)
                {
                    Interlocked.Increment(ref _hits);
                    var value = JsonSerializer.Deserialize<T>(l2Bytes);

                    if (value is not null && _options.Hybrid.PromoteOnHit && _options.Hybrid.L1Enabled)
                        SetL1(key, JsonSerializer.Serialize(value));

                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis L2 read failed for key {Key}; falling back to miss", key);
            }
        }

        Interlocked.Increment(ref _misses);

        if (_options.Diagnostics.LogCacheMisses)
            _logger.LogDebug("Cache miss for key: {Key}", key);

        return null;
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var ttl = expiration ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
        var serialized = JsonSerializer.Serialize(value);

        if (_options.Hybrid.L1Enabled)
            SetL1(key, serialized, ttl);

        if (_options.Hybrid.L2Enabled && _l2 is not null)
        {
            try
            {
                var redisOpts = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                };
                await _l2.SetAsync(PrefixKey(key), JsonSerializer.SerializeToUtf8Bytes(value), redisOpts, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis L2 write failed for key {Key}; L1 only", key);
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);

        if (_options.Hybrid.L1Enabled)
        {
            _l1.Remove(key);
            _l1Keys.TryRemove(key, out _);
        }

        if (_options.Hybrid.L2Enabled && _l2 is not null)
        {
            try
            {
                await _l2.RemoveAsync(PrefixKey(key), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis L2 remove failed for key {Key}", key);
            }
        }

        _locks.TryRemove(key, out var sem);
        sem?.Dispose();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);

        if (_options.Hybrid.L1Enabled && _l1.TryGetValue(key, out _))
            return true;

        if (_options.Hybrid.L2Enabled && _l2 is not null)
        {
            try
            {
                return await _l2.GetAsync(PrefixKey(key), ct).ConfigureAwait(false) is not null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis L2 exists check failed for key {Key}", key);
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class
    {
        ValidateKey(key);

        var cached = await GetAsync<T>(key, ct).ConfigureAwait(false);
        if (cached is not null) return cached;

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            cached = await GetAsync<T>(key, ct).ConfigureAwait(false);
            if (cached is not null) return cached;

            var fresh = await factory(ct).ConfigureAwait(false);
            await SetAsync(key, fresh, expiration, ct).ConfigureAwait(false);
            return fresh;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);

        var result = new Dictionary<string, T>();
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, ct).ConfigureAwait(false);
            if (value is not null) result[key] = value;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task SetManyAsync<T>(
        IDictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var (key, value) in items)
            await SetAsync(key, value, expiration, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default)
    {
        if (_options.Hybrid.L1Enabled)
        {
            var toRemove = _l1Keys.Keys
                .Where(k => MatchesGlob(k, pattern))
                .ToList();

            foreach (var key in toRemove)
            {
                _l1.Remove(key);
                _l1Keys.TryRemove(key, out _);
            }
        }

        // Redis pattern invalidation requires SCAN — not available via IDistributedCache.
        // When Redis is in use, callers should use the Redis-aware tier directly.
        if (_options.Hybrid.L2Enabled && _l2 is not null)
        {
            _logger.LogWarning(
                "InvalidateByPatternAsync with Redis: pattern scan not supported via IDistributedCache. " +
                "Only L1 was invalidated for pattern {Pattern}", pattern);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken ct = default)
    {
        foreach (var key in _l1Keys.Keys) _l1.Remove(key);
        _l1Keys.Clear();
        _lastCleared = DateTimeOffset.UtcNow;
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);

        // IDistributedCache has no bulk clear; log a warning.
        if (_options.Hybrid.L2Enabled && _l2 is not null)
        {
            _logger.LogWarning(
                "ClearAsync does not support Redis bulk-clear via IDistributedCache. L1 was cleared only.");
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);
        var total = hits + misses;
        var hitRate = total == 0 ? 0.0 : (double)hits / total;

        return Task.FromResult(new CacheStatistics(
            Hits: hits,
            Misses: misses,
            HitRate: hitRate,
            Size: _l1Keys.Count,
            LastCleared: _lastCleared));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var sem in _locks.Values) sem.Dispose();
        _locks.Clear();
    }

    private void SetL1(string key, string serialized, TimeSpan? ttl = null)
    {
        var opts = new MemoryCacheEntryOptions { Size = 1 };

        var effectiveTtl = ttl ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
        opts.AbsoluteExpirationRelativeToNow = effectiveTtl;

        if (_options.Memory.SlidingExpiration > 0)
            opts.SlidingExpiration = TimeSpan.FromSeconds(_options.Memory.SlidingExpiration);

        _l1.Set(key, serialized, opts);
        _l1Keys.TryAdd(key, 0);
    }

    private string PrefixKey(string key)
    {
        var prefix = _options.Redis.KeyPrefix;
        return string.IsNullOrEmpty(prefix) ? key : $"{prefix}{key}";
    }

    private void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > _options.CacheKeyPolicy.MaxKeyLength)
            throw new ArgumentException(
                $"Cache key exceeds maximum length of {_options.CacheKeyPolicy.MaxKeyLength} characters.",
                nameof(key));
    }

    private static bool MatchesGlob(string input, string pattern)
    {
        var parts = pattern.Split('*');
        if (parts.Length == 1) return input.Equals(pattern, StringComparison.Ordinal);

        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0) continue;

            var idx = input.IndexOf(part, pos, StringComparison.Ordinal);
            if (idx < 0) return false;

            if (i == 0 && idx != 0) return false;
            pos = idx + part.Length;
        }

        return !pattern.EndsWith('*') ? pos == input.Length : true;
    }
}
