namespace SharedCommon.HealthChecks;

/// <summary>Configuration for the SharedCommon health check infrastructure.</summary>
public sealed class HealthCheckOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:HealthChecks";

    /// <summary>Default timeout applied to each individual health check. Defaults to 5 seconds.</summary>
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Redis connectivity check configuration.</summary>
    public RedisCheckOptions? Redis { get; init; }

    /// <summary>External HTTP dependency checks.</summary>
    public IReadOnlyList<ExternalHttpCheckOptions> ExternalHttp { get; init; } = [];
}

/// <summary>Options for the Redis health check.</summary>
public sealed class RedisCheckOptions
{
    /// <summary>Whether to register the Redis health check.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Name reported in the health report. Defaults to "redis".</summary>
    public string Name { get; init; } = "redis";
}

/// <summary>Options for a single external HTTP dependency check.</summary>
public sealed class ExternalHttpCheckOptions
{
    /// <summary>Name reported in the health report.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Absolute URI of the endpoint to probe (e.g., the <c>/health</c> path of a dependency).</summary>
    public string Uri { get; init; } = string.Empty;
}
