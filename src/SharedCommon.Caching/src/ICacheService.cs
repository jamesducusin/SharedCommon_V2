namespace SharedCommon.Caching;

/// <summary>
/// Unified caching interface that hides the underlying cache topology (L1 → L2 → L3).
/// All operations are safe to call even when lower tiers are unavailable; the service
/// degrades gracefully and logs warnings rather than throwing.
///
/// Key convention: <c>{package}:{entity}:{id}</c> — for example <c>users:profile:42</c>.
///
/// Example:
/// <code>
/// var user = await cache.GetOrSetAsync(
///     key: "users:profile:42",
///     factory: ct => userRepo.GetAsync(42, ct),
///     expiration: TimeSpan.FromMinutes(30),
///     ct: ct);
/// </code>
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a value from the cache (L1 → L2 → L3).
    /// Returns <c>null</c> on a miss or deserialisation failure.
    /// </summary>
    /// <typeparam name="T">Reference type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Writes a value to all enabled cache tiers.
    /// Uses <c>DefaultTtlSeconds</c> when <paramref name="expiration"/> is omitted.
    /// </summary>
    /// <typeparam name="T">Reference type of the value to cache.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to store.</param>
    /// <param name="expiration">Entry lifetime. <c>null</c> uses configured default.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Removes the key from all enabled cache tiers.
    /// No-ops silently if the key does not exist.
    /// </summary>
    /// <param name="key">Cache key to invalidate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if the key exists in any enabled tier.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Get-or-set with stampede protection.
    /// Only one concurrent caller executes <paramref name="factory"/>; all others wait for the result.
    /// </summary>
    /// <typeparam name="T">Reference type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Async factory invoked on a cache miss.</param>
    /// <param name="expiration">Entry lifetime. <c>null</c> uses configured default.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached or freshly computed value. Never null (factory must return a value).</returns>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Retrieves multiple keys in a single batch operation.
    /// Missing keys are absent from the returned dictionary.
    /// </summary>
    /// <typeparam name="T">Reference type of cached values.</typeparam>
    /// <param name="keys">Cache keys.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary of found key–value pairs.</returns>
    Task<IDictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Writes multiple key–value pairs in a single batch operation.
    /// </summary>
    /// <typeparam name="T">Reference type of values to cache.</typeparam>
    /// <param name="items">Key–value pairs to store.</param>
    /// <param name="expiration">Entry lifetime. <c>null</c> uses configured default.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetManyAsync<T>(
        IDictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Removes all keys that match the given glob pattern.
    /// Supported by Redis (KEYS/SCAN); falls back to a no-op for memory-only setups.
    /// </summary>
    /// <param name="pattern">Glob pattern, e.g. <c>order:123:*</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default);

    /// <summary>
    /// Removes all entries from all enabled tiers.
    /// Use with caution — this is a destructive operation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns accumulated hit/miss statistics for this service instance.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default);
}
