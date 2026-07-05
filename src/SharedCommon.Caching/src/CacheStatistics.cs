namespace SharedCommon.Caching;

/// <summary>
/// Snapshot of cache hit/miss counters for a single <see cref="ICacheService"/> instance.
/// Produced by <see cref="ICacheService.GetStatisticsAsync"/>.
/// </summary>
/// <param name="Hits">Total cache hits since the service was last cleared or started.</param>
/// <param name="Misses">Total cache misses since the service was last cleared or started.</param>
/// <param name="HitRate">Ratio of hits to total lookups (<c>0.0–1.0</c>). <c>0.0</c> when no lookups have occurred.</param>
/// <param name="Size">Approximate number of entries currently in the cache.</param>
/// <param name="LastCleared">Timestamp of the most recent <c>ClearAsync</c> call, or the service start time if never cleared.</param>
public record CacheStatistics(
    long Hits,
    long Misses,
    double HitRate,
    long Size,
    DateTimeOffset LastCleared);
