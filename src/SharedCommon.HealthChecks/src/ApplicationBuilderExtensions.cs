using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using AspNetHealthCheckOptions = Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions;

namespace SharedCommon.HealthChecks;

/// <summary>Endpoint mapping extensions for SharedCommon health checks.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Maps <c>/health/live</c> (liveness — fast, no external deps) and
    /// <c>/health/ready</c> (readiness — checks all dependencies) endpoints.
    /// Response format is driven by <see cref="IHealthCheckReporter"/>.
    /// </summary>
    public static IEndpointRouteBuilder UseSharedHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reporter = endpoints.ServiceProvider.GetRequiredService<IHealthCheckReporter>();

        endpoints.MapHealthChecks("/health/live", new AspNetHealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = (ctx, report) =>
                reporter.WriteReportAsync(ctx, report, ctx.RequestAborted)
        });

        endpoints.MapHealthChecks("/health/ready", new AspNetHealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("live"),
            ResponseWriter = (ctx, report) =>
                reporter.WriteReportAsync(ctx, report, ctx.RequestAborted)
        });

        return endpoints;
    }
}
