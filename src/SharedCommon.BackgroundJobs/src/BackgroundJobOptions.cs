using System.ComponentModel.DataAnnotations;

namespace SharedCommon.BackgroundJobs;

/// <summary>Configuration for the SharedCommon background job infrastructure.</summary>
public sealed class BackgroundJobOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:BackgroundJobs";

    /// <summary>
    /// Storage backend for job state. Defaults to <see cref="JobStorageBackend.InMemory"/>.
    /// Use <see cref="JobStorageBackend.SqlServer"/> (or another persistent backend)
    /// in production so jobs survive restarts.
    /// </summary>
    public JobStorageBackend Backend { get; init; } = JobStorageBackend.InMemory;

    /// <summary>
    /// SQL Server connection string. Required when <see cref="Backend"/> is <see cref="JobStorageBackend.SqlServer"/>.
    /// Use User Secrets or a secrets manager — never put connection strings in appsettings.json.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Number of Hangfire worker threads.
    /// Defaults to 5.
    /// </summary>
    [Range(1, 200)]
    public int WorkerCount { get; init; } = 5;

    /// <summary>
    /// Prefix for Hangfire queue names. Defaults to "default".
    /// </summary>
    public string DefaultQueue { get; init; } = "default";

    /// <summary>
    /// Whether to expose the Hangfire dashboard (at /jobs).
    /// Secure it with authorization — never enable without auth in production.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableDashboard { get; init; } = false;

    /// <summary>
    /// Dashboard route. Defaults to "/jobs".
    /// Only relevant when <see cref="EnableDashboard"/> is <c>true</c>.
    /// </summary>
    public string DashboardPath { get; init; } = "/jobs";

    /// <summary>
    /// Role required to access the Hangfire dashboard.
    /// Empty string = no auth check (development only).
    /// </summary>
    public string DashboardRequiredRole { get; init; } = string.Empty;
}

/// <summary>Supported Hangfire storage backends.</summary>
public enum JobStorageBackend
{
    /// <summary>
    /// In-memory storage (default). Jobs are lost on restart.
    /// Suitable for development and simple single-instance scenarios.
    /// </summary>
    InMemory,

    /// <summary>
    /// SQL Server persistent storage. Jobs survive restarts.
    /// Requires <see cref="BackgroundJobOptions.ConnectionString"/>.
    /// </summary>
    SqlServer
}
