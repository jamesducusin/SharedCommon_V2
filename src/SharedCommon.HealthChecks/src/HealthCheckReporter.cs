using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SharedCommon.HealthChecks;

/// <summary>
/// Default <see cref="IHealthCheckReporter"/> that writes a structured JSON report.
/// Excludes exception details from public responses to prevent information leakage.
/// </summary>
public sealed class HealthCheckReporter : IHealthCheckReporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public async Task WriteReportAsync(
        HttpContext context,
        HealthReport report,
        CancellationToken cancellationToken = default)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => StatusCodes.Status200OK,
            _ => StatusCodes.Status503ServiceUnavailable
        };

        var body = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags
            })
        };

        await JsonSerializer.SerializeAsync(
            context.Response.Body, body, JsonOptions, cancellationToken);
    }
}
