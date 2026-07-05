using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedCommon.HealthChecks.Checks;

/// <summary>
/// Health check that verifies Redis connectivity via <see cref="IDistributedCache"/>.
/// Writes and reads a short-lived probe key, then removes it.
/// </summary>
public sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    private const string ProbeKey = "__health_probe__";

    private static readonly DistributedCacheEntryOptions ProbeOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
    };

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetStringAsync(ProbeKey, "1", ProbeOptions, cancellationToken);
            var value = await cache.GetStringAsync(ProbeKey, cancellationToken);
            await cache.RemoveAsync(ProbeKey, cancellationToken);

            return value == "1"
                ? HealthCheckResult.Healthy("Redis is reachable.")
                : HealthCheckResult.Degraded("Redis did not return the expected probe value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", ex);
        }
    }
}
