namespace Templates.Api.Endpoints;

using Templates.Api.Infrastructure.HealthChecks;

/// <summary>
/// Health check endpoints for Kubernetes probes and monitoring.
/// Provides liveness (app is running) and readiness (app is ready for traffic) checks.
/// Also provides detailed health information about all dependencies.
/// </summary>
public static class HealthEndpoint
{
    /// <summary>
    /// Maps health check endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapHealthEndpoint(this WebApplication app)
    {
        var group = app.MapGroup("/health")
            .WithName("Health")
            .WithTags("Health")
            .WithOpenApi()
            .WithoutAuthorization();

        // Liveness probe: App is running
        group.MapGet("/live", GetHealthLive)
            .WithName("HealthLive")
            .WithSummary("Liveness probe — indicates if app process is alive")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        // Readiness probe: App is ready to accept traffic
        group.MapGet("/ready", GetHealthReady)
            .WithName("HealthReady")
            .WithSummary("Readiness probe — indicates if app is ready to handle requests")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        // Detailed health information
        group.MapGet("/detailed", GetHealthDetailed)
            .WithName("HealthDetailed")
            .WithSummary("Detailed health check — shows status of all dependencies")
            .Produces<HealthCheckResponse>(StatusCodes.Status200OK)
            .Produces<HealthCheckResponse>(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Liveness probe: Kubernetes checks this to determine if pod should be restarted.
    /// Should only return false if the app process is hung or dead.
    /// </summary>
    private static async Task<IResult> GetHealthLive(
        IHealthCheckService healthService,
        CancellationToken ct)
    {
        var isLive = await healthService.IsLiveAsync(ct);
        
        if (!isLive)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe: Kubernetes checks this to determine if pod should receive traffic.
    /// Returns false if app is starting up, dependencies unavailable, or shutting down.
    /// </summary>
    private static async Task<IResult> GetHealthReady(
        IHealthCheckService healthService,
        CancellationToken ct)
    {
        var isReady = await healthService.IsReadyAsync(ct);
        
        if (!isReady)
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check: Returns comprehensive health status of all dependencies.
    /// Use for monitoring dashboards, alerting, and diagnostics.
    /// </summary>
    private static async Task<IResult> GetHealthDetailed(
        IHealthCheckService healthService,
        CancellationToken ct)
    {
        var health = await healthService.CheckHealthAsync(ct);

        // Return appropriate HTTP status based on overall health
        var statusCode = health.Status switch
        {
            "healthy" => StatusCodes.Status200OK,
            "degraded" => StatusCodes.Status200OK,  // Still OK, but not ideal
            "unhealthy" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status503ServiceUnavailable
        };

        return Results.Json(health, statusCode: statusCode);
    }
}

// Required using statements
using Templates.Api.Common.Models;
