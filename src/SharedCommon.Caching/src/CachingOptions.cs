using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Caching;

/// <summary>
/// Top-level configuration for the SharedCommon caching layer.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "Caching": {
///       "DefaultProvider": "Hybrid",
///       "DefaultTtlSeconds": 300,
///       "Redis": {
///         "Enabled": true,
///         "Connection": "localhost:6379"
///       }
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class CachingOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Caching</c>.</summary>
    public const string SectionName = "SharedCommon:Caching";

    /// <summary>
    /// Active caching provider.
    /// Valid values: <c>Memory</c>, <c>Redis</c>, <c>Hybrid</c>.
    /// Default: <c>Hybrid</c>.
    /// </summary>
    public string DefaultProvider { get; set; } = "Hybrid";

    /// <summary>Default TTL in seconds applied when no expiration is specified. Default: 300.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "DefaultTtlSeconds must be positive.")]
    public int DefaultTtlSeconds { get; set; } = 300;

    /// <summary>Serialization format for distributed cache entries. <c>Json</c> | <c>MessagePack</c>. Default: <c>Json</c>.</summary>
    public string SerializationFormat { get; set; } = "Json";

    /// <summary>In-memory (L1) tier settings.</summary>
    public MemoryCacheOptions Memory { get; set; } = new();

    /// <summary>Redis (L2) tier settings.</summary>
    public RedisCacheOptions Redis { get; set; } = new();

    /// <summary>Database (L3) tier settings.</summary>
    public DatabaseCacheOptions Database { get; set; } = new();

    /// <summary>Cache key construction policy.</summary>
    public CacheKeyPolicyOptions CacheKeyPolicy { get; set; } = new();

    /// <summary>Hybrid cache tier orchestration settings.</summary>
    public HybridCacheOptions Hybrid { get; set; } = new();

    /// <summary>Diagnostics and statistics settings.</summary>
    public CacheDiagnosticsOptions Diagnostics { get; set; } = new();
}

/// <summary>In-memory (L1) tier configuration.</summary>
public sealed class MemoryCacheOptions
{
    /// <summary>Enable the in-memory tier. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum number of items before LRU eviction. Default: 10000.</summary>
    [Range(1, int.MaxValue)]
    public int MaximumSize { get; set; } = 10_000;

    /// <summary>Sliding expiration in seconds. Resets on access. Default: 300.</summary>
    public int SlidingExpiration { get; set; } = 300;

    /// <summary>Absolute expiration in seconds. <c>null</c> disables absolute expiry. Default: null.</summary>
    public int? AbsoluteExpiration { get; set; }
}

/// <summary>Redis (L2) tier configuration.</summary>
public sealed class RedisCacheOptions
{
    /// <summary>Enable the Redis tier. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>StackExchange.Redis connection string. Required when enabled.</summary>
    public string Connection { get; set; } = string.Empty;

    /// <summary>Key prefix applied to all Redis keys. Default: <c>sharedcommon:</c>.</summary>
    public string KeyPrefix { get; set; } = "sharedcommon:";

    /// <summary>Default TTL in seconds for Redis entries. Default: 300.</summary>
    [Range(1, int.MaxValue)]
    public int DefaultTtlSeconds { get; set; } = 300;

    /// <summary>Redis logical database index (0–15). Default: 0.</summary>
    [Range(0, 15)]
    public int DatabaseId { get; set; } = 0;

    /// <summary>Enable SSL/TLS for Redis connections. Default: <c>false</c>.</summary>
    public bool Ssl { get; set; } = false;

    /// <summary>Connect timeout in milliseconds. Default: 5000.</summary>
    [Range(100, int.MaxValue)]
    public int ConnectTimeout { get; set; } = 5_000;

    /// <summary>Sync timeout in milliseconds. Default: 1000.</summary>
    [Range(100, int.MaxValue)]
    public int SyncTimeout { get; set; } = 1_000;
}

/// <summary>Database (L3) tier configuration.</summary>
public sealed class DatabaseCacheOptions
{
    /// <summary>Enable the database tier. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>ADO.NET connection string. Required when enabled.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Table name for cache entries. Default: <c>CacheItems</c>.</summary>
    public string TableName { get; set; } = "CacheItems";

    /// <summary>Default TTL in seconds for database tier entries. Default: 300.</summary>
    [Range(1, int.MaxValue)]
    public int DefaultTtlSeconds { get; set; } = 300;

    /// <summary>Interval in seconds between expired-row cleanup jobs. Default: 3600.</summary>
    [Range(60, int.MaxValue)]
    public int CleanupIntervalSeconds { get; set; } = 3_600;
}

/// <summary>Cache key construction policy.</summary>
public sealed class CacheKeyPolicyOptions
{
    /// <summary>Separator between key segments. Default: <c>:</c>.</summary>
    public string Separator { get; set; } = ":";

    /// <summary>Lowercase all cache keys for consistency. Default: <c>true</c>.</summary>
    public bool NormalizeKeys { get; set; } = true;

    /// <summary>Maximum allowed key length in bytes. Default: 512.</summary>
    [Range(1, 4096)]
    public int MaxKeyLength { get; set; } = 512;
}

/// <summary>Hybrid cache tier orchestration settings.</summary>
public sealed class HybridCacheOptions
{
    /// <summary>Enable the L1 (memory) tier in hybrid mode. Default: <c>true</c>.</summary>
    public bool L1Enabled { get; set; } = true;

    /// <summary>Enable the L2 (Redis) tier in hybrid mode. Default: <c>false</c>.</summary>
    public bool L2Enabled { get; set; } = false;

    /// <summary>Enable the L3 (database) tier in hybrid mode. Default: <c>false</c>.</summary>
    public bool L3Enabled { get; set; } = false;

    /// <summary>On an L2/L3 hit, populate L1 to avoid repeated network hops. Default: <c>true</c>.</summary>
    public bool PromoteOnHit { get; set; } = true;

    /// <summary>When a key is removed, propagate the invalidation to all enabled tiers. Default: <c>true</c>.</summary>
    public bool InvalidateDownstream { get; set; } = true;
}

/// <summary>Diagnostics and statistics settings.</summary>
public sealed class CacheDiagnosticsOptions
{
    /// <summary>Enable diagnostics collection. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Track hit/miss statistics for <see cref="ICacheService.GetStatisticsAsync"/>. Default: <c>true</c>.</summary>
    public bool TrackStatistics { get; set; } = true;

    /// <summary>Emit a log entry for every cache miss. Default: <c>false</c>.</summary>
    public bool LogCacheMisses { get; set; } = false;
}
