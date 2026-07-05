using System.Diagnostics;

namespace SharedCommon.Observability;

/// <summary>
/// Central registry of <see cref="ActivitySource"/> instances for all SharedCommon packages.
/// Use these when creating spans to ensure consistent source naming and version.
/// </summary>
public static class ActivitySources
{
    private const string Version = "1.0.0";

    /// <summary>ActivitySource for SharedCommon.Core operations.</summary>
    public static readonly ActivitySource Core = new("SharedCommon.Core", Version);

    /// <summary>ActivitySource for SharedCommon.Caching operations (cache get/set/invalidate).</summary>
    public static readonly ActivitySource Caching = new("SharedCommon.Caching", Version);

    /// <summary>ActivitySource for SharedCommon.Auth operations (token issue/validate/revoke).</summary>
    public static readonly ActivitySource Auth = new("SharedCommon.Auth", Version);

    /// <summary>ActivitySource for SharedCommon.Messaging operations (publish/consume).</summary>
    public static readonly ActivitySource Messaging = new("SharedCommon.Messaging", Version);

    /// <summary>ActivitySource for SharedCommon.Resiliency operations (retry/circuit-breaker events).</summary>
    public static readonly ActivitySource Resiliency = new("SharedCommon.Resiliency", Version);

    /// <summary>Returns all ActivitySource names, for use when configuring the TracerProvider.</summary>
    public static IReadOnlyList<string> AllSourceNames =>
    [
        Core.Name,
        Caching.Name,
        Auth.Name,
        Messaging.Name,
        Resiliency.Name,
    ];
}
