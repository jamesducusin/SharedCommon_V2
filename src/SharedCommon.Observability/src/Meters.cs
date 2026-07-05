using System.Diagnostics.Metrics;

namespace SharedCommon.Observability;

/// <summary>
/// Central registry of <see cref="Meter"/> instances and standard instruments for SharedCommon packages.
/// </summary>
public static class Meters
{
    private const string Version = "1.0.0";

    /// <summary>Meter covering caching operations.</summary>
    public static readonly Meter Caching = new("SharedCommon.Caching", Version);

    /// <summary>Meter covering authentication operations.</summary>
    public static readonly Meter Auth = new("SharedCommon.Auth", Version);

    /// <summary>Meter covering messaging operations.</summary>
    public static readonly Meter Messaging = new("SharedCommon.Messaging", Version);

    // ── Caching instruments ───────────────────────────────────────────────────

    /// <summary>Number of cache hits (key found in cache).</summary>
    public static readonly Counter<long> CacheHits =
        Caching.CreateCounter<long>("cache.hits", description: "Number of cache hits.");

    /// <summary>Number of cache misses (key not found in cache).</summary>
    public static readonly Counter<long> CacheMisses =
        Caching.CreateCounter<long>("cache.misses", description: "Number of cache misses.");

    // ── Auth instruments ──────────────────────────────────────────────────────

    /// <summary>Number of tokens issued.</summary>
    public static readonly Counter<long> TokensIssued =
        Auth.CreateCounter<long>("auth.tokens_issued", description: "Number of JWT tokens issued.");

    /// <summary>Number of token validation failures.</summary>
    public static readonly Counter<long> TokenValidationFailures =
        Auth.CreateCounter<long>("auth.token_validation_failures",
            description: "Number of failed JWT token validations.");

    // ── Messaging instruments ─────────────────────────────────────────────────

    /// <summary>Number of messages published.</summary>
    public static readonly Counter<long> MessagesPublished =
        Messaging.CreateCounter<long>("messaging.published", description: "Number of messages published.");

    /// <summary>Number of messages consumed.</summary>
    public static readonly Counter<long> MessagesConsumed =
        Messaging.CreateCounter<long>("messaging.consumed", description: "Number of messages consumed.");

    /// <summary>Returns all meter names, for use when configuring the MeterProvider.</summary>
    public static IReadOnlyList<string> AllMeterNames =>
    [
        Caching.Name,
        Auth.Name,
        Messaging.Name,
    ];
}
