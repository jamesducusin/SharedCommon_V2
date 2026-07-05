using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SharedCommon.Caching;

/// <summary>
/// <see cref="ICacheService"/> implementation backed exclusively by <see cref="IMemoryCache"/> (L1).
/// Thread-safe. Stampede protection uses per-key <see cref="SemaphoreSlim"/> locks.
/// Used when <c>DefaultProvider = Memory</c> and as the L1 tier inside <see cref="HybridCacheService"/>.
/// </summary>
public sealed class InMemoryCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _options;
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    private long _hits;
    private long _misses;
    private DateTimeOffset _lastCleared = DateTimeOffset.UtcNow;

    /// <summary>Initializes the in-memory cache service.</summary>
    public InMemoryCacheService(
        IMemoryCache cache,
        IOptions<CachingOptions> options,
        ILogger<InMemoryCacheService> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        ValidateKey(key);

        if (_cache.TryGetValue(key, out string? serialized) && serialized is not null)
        {
            Interlocked.Increment(ref _hits);
            var value = JsonSerializer.Deserialize<T>(serialized);
            return Task.FromResult(value);
        }

        Interlocked.Increment(ref _misses);

        if (_options.Diagnostics.LogCacheMisses)
            _logger.LogDebug("Cache miss for key: {Key}", key);

        return Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var ttl = expiration ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
        var serialized = JsonSerializer.Serialize(value);

        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Size = 1
        };

        _cache.Set(key, serialized, entryOptions);
        _keys.TryAdd(key, 0);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        _locks.TryRemove(key, out var sem);
        sem?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);
        return Task.FromResult(_cache.TryGetValue(key, out _));
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
            // Double-check after acquiring the lock.
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
        // IMemoryCache has no native pattern scan; iterate tracked keys.
        var toRemove = _keys.Keys
            .Where(k => MatchesGlob(k, pattern))
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken ct = default)
    {
        foreach (var key in _keys.Keys)
        {
            _cache.Remove(key);
        }
        _keys.Clear();
        _lastCleared = DateTimeOffset.UtcNow;
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
        return Task.CompletedTask;
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
            Size: _keys.Count,
            LastCleared: _lastCleared));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var sem in _locks.Values) sem.Dispose();
        _locks.Clear();
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
        // Simple * wildcard only — sufficient for key pattern invalidation.
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
