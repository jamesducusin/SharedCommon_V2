using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedCommon.HealthChecks;

/// <summary>
/// Writes a structured health report to the HTTP response.
/// Implement this to customize the JSON format returned by <c>/health/live</c> and <c>/health/ready</c>.
/// </summary>
public interface IHealthCheckReporter
{
    /// <summary>
    /// Serializes <paramref name="report"/> to the response stream.
    /// Must set <c>Content-Type: application/json</c> and the HTTP status code.
    /// </summary>
    Task WriteReportAsync(HttpContext context, HealthReport report, CancellationToken cancellationToken = default);
}
