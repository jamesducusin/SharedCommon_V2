namespace Templates.Api.Infrastructure.HealthChecks;

using System.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Templates.Api.Common.Models;

/// <summary>
/// Service for performing detailed health checks on application dependencies.
/// Checks database, cache, external services, and reports overall health status.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Perform a detailed health check of all dependencies.
    /// </summary>
    Task<HealthCheckResponse> CheckHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Quick liveness check (app is running)
    /// </summary>
    Task<bool> IsLiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Readiness check (app is ready for traffic)
    /// </summary>
    Task<bool> IsReadyAsync(CancellationToken ct = default);
}

/// <summary>
/// Default implementation of IHealthCheckService.
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(IConfiguration configuration, ILogger<HealthCheckService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResponse> CheckHealthAsync(CancellationToken ct = default)
    {
        var checks = new Dictionary<string, DependencyHealthStatus>();
        var sw = Stopwatch.StartNew();

        try
        {
            // Check database connectivity
            var dbStatus = await CheckDatabaseAsync(ct);
            checks["database"] = dbStatus;

            // Check cache connectivity (if enabled)
            if (_configuration.GetValue<bool>("Features:Caching:Enabled"))
            {
                var cacheStatus = await CheckCacheAsync(ct);
                checks["cache"] = cacheStatus;
            }

            // Check messaging connectivity (if enabled)
            if (_configuration.GetValue<bool>("Features:Messaging:Enabled"))
            {
                var messagingStatus = await CheckMessagingAsync(ct);
                checks["messaging"] = messagingStatus;
            }

            // Determine overall status
            var overallStatus = DetermineOverallStatus(checks);
            
            // Calculate health score
            var healthScore = CalculateHealthScore(checks);

            sw.Stop();

            _logger.LogInformation(
                "Health check completed: {Status}, {CheckCount} checks in {ElapsedMs}ms",
                overallStatus, checks.Count, sw.ElapsedMilliseconds);

            return new HealthCheckResponse(
                Status: overallStatus,
                Checks: checks,
                Timestamp: DateTime.UtcNow,
                HealthScore: healthScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");

            return new HealthCheckResponse(
                Status: "unhealthy",
                Checks: checks,
                Timestamp: DateTime.UtcNow,
                HealthScore: 0);
        }
    }

    public async Task<bool> IsLiveAsync(CancellationToken ct = default)
    {
        try
        {
            // Liveness is simple: app is running
            // Return true if we can execute this method
            await Task.Delay(10, ct); // Simulate minimal work
            return true;
        }
        catch (OperationCanceledException)
        {
            return true; // Still live, just cancelled
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsReadyAsync(CancellationToken ct = default)
    {
        try
        {
            // Readiness requires database to be available
            var dbStatus = await CheckDatabaseAsync(ct);
            
            if (dbStatus.Status != "healthy")
            {
                _logger.LogWarning("Readiness check failed: database is {DbStatus}", dbStatus.Status);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return false;
        }
    }

    private async Task<DependencyHealthStatus> CheckDatabaseAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            return new DependencyHealthStatus(
                Status: "unhealthy",
                Message: "Database connection string not configured",
                Details: new() { { "reason", "missing_connection_string" } });
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);
            
            // Execute a simple health check query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5; // 5 second timeout

            await command.ExecuteScalarAsync(ct);

            sw.Stop();

            return new DependencyHealthStatus(
                Status: "healthy",
                Message: "Database connection successful",
                ResponseTimeMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogWarning(ex, "Database health check failed in {ElapsedMs}ms", sw.ElapsedMilliseconds);

            return new DependencyHealthStatus(
                Status: "unhealthy",
                Message: $"Database connection failed: {ex.Message}",
                ResponseTimeMs: sw.ElapsedMilliseconds,
                Details: new() { { "exception", ex.GetType().Name } });
        }
    }

    private async Task<DependencyHealthStatus> CheckCacheAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Note: This is a placeholder. When SharedCommon.Caching is integrated,
            // inject ICacheService and use it to verify connectivity.
            // For now, return a status indicating cache is not fully integrated.

            sw.Stop();

            return new DependencyHealthStatus(
                Status: "degraded",
                Message: "Cache integration pending (SharedCommon.Caching)",
                ResponseTimeMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();

            return new DependencyHealthStatus(
                Status: "unhealthy",
                Message: $"Cache check failed: {ex.Message}",
                ResponseTimeMs: sw.ElapsedMilliseconds,
                Details: new() { { "exception", ex.GetType().Name } });
        }
    }

    private async Task<DependencyHealthStatus> CheckMessagingAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Note: This is a placeholder. When SharedCommon.Messaging is integrated,
            // inject IMessageBus and use it to verify connectivity.
            // For now, return a status indicating messaging is not fully integrated.

            sw.Stop();

            return new DependencyHealthStatus(
                Status: "degraded",
                Message: "Messaging integration pending (SharedCommon.Messaging)",
                ResponseTimeMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();

            return new DependencyHealthStatus(
                Status: "unhealthy",
                Message: $"Messaging check failed: {ex.Message}",
                ResponseTimeMs: sw.ElapsedMilliseconds,
                Details: new() { { "exception", ex.GetType().Name } });
        }
    }

    private static string DetermineOverallStatus(Dictionary<string, DependencyHealthStatus> checks)
    {
        if (checks.Count == 0)
            return "unknown";

        // If any critical check is unhealthy, overall is unhealthy
        if (checks.Any(c => c.Key == "database" && c.Value.Status == "unhealthy"))
            return "unhealthy";

        // If any check is unhealthy, overall is degraded
        if (checks.Any(c => c.Value.Status == "unhealthy"))
            return "degraded";

        // If any check is degraded, overall is degraded
        if (checks.Any(c => c.Value.Status == "degraded"))
            return "degraded";

        // All checks are healthy
        return "healthy";
    }

    private static int CalculateHealthScore(Dictionary<string, DependencyHealthStatus> checks)
    {
        if (checks.Count == 0)
            return 0;

        var scores = checks.Values.Select(c => c.Status switch
        {
            "healthy" => 100,
            "degraded" => 50,
            "unhealthy" => 0,
            _ => 0
        }).ToList();

        return (int)(scores.Average());
    }
}
