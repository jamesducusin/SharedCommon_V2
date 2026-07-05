using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedCommon.HealthChecks.Checks;

/// <summary>
/// Health check that probes an external HTTP dependency with a HEAD request.
/// Register via <c>services.AddHttpClient&lt;ExternalHttpHealthCheck&gt;(name, c => c.BaseAddress = uri)</c>.
/// </summary>
public sealed class ExternalHttpHealthCheck(HttpClient httpClient) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "/");
            using var response = await httpClient.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"HTTP dependency responded {(int)response.StatusCode}.")
                : HealthCheckResult.Degraded($"HTTP dependency returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("HTTP dependency is unreachable.", ex);
        }
    }
}
