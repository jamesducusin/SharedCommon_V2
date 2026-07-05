namespace Templates.Api.Common.Models;

/// <summary>
/// Detailed health status for the application and its dependencies.
/// Used by Kubernetes liveness/readiness probes and monitoring dashboards.
/// </summary>
public record HealthCheckResponse(
    /// <summary>
    /// Overall status: "healthy", "degraded", or "unhealthy"
    /// </summary>
    string Status,
    
    /// <summary>
    /// Individual dependency checks
    /// </summary>
    Dictionary<string, DependencyHealthStatus> Checks,
    
    /// <summary>
    /// UTC timestamp when health check was performed
    /// </summary>
    DateTime Timestamp = default,
    
    /// <summary>
    /// Optional: Overall health score (0-100)
    /// </summary>
    int? HealthScore = null);

/// <summary>
/// Health status of a single dependency.
/// </summary>
public record DependencyHealthStatus(
    /// <summary>
    /// Status of this dependency: "healthy", "degraded", "unhealthy"
    /// </summary>
    string Status,
    
    /// <summary>
    /// Human-readable status message
    /// </summary>
    string Message,
    
    /// <summary>
    /// Optional: Response time in milliseconds
    /// </summary>
    long? ResponseTimeMs = null,
    
    /// <summary>
    /// Optional: Additional context (connection count, pool size, error details)
    /// </summary>
    Dictionary<string, object>? Details = null);
