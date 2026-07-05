namespace Templates.Infrastructure.Persistence.Migrations;

using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;

/// <summary>
/// Database schema initialization and migration using DbUp.
/// Manages schema versioning, stored procedures, and data seeding.
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
    /// Apply all pending migrations to the database.
    /// Call this during application startup.
    /// </summary>
    Task<bool> MigrateAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if database has pending migrations.
    /// </summary>
    Task<bool> HasPendingMigrationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get list of applied migrations.
    /// </summary>
    Task<List<string>> GetAppliedMigrationsAsync(CancellationToken ct = default);
}

/// <summary>
/// Default implementation using DbUp for migrations.
/// </summary>
public class DbUpMigrationService : IDatabaseMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<DbUpMigrationService> _logger;

    public DbUpMigrationService(
        IConfiguration configuration,
        ILogger<DbUpMigrationService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        _logger = logger;
    }

    public async Task<bool> MigrateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting database migrations...");

            var upgrader = DeployChanges
                .To
                .SqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(
                    Assembly.GetExecutingAssembly(),
                    s => s.StartsWith("Templates.Infrastructure.Persistence.Migrations.Scripts"))
                .WithTransactionPerScript()
                .LogScriptOutput()
                .LogToNowhere()  // We'll handle logging separately
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                _logger.LogError(result.Error, "Database migration failed");
                return false;
            }

            _logger.LogInformation("Database migrations completed successfully");
            _logger.LogInformation("Applied scripts: {Scripts}",
                string.Join(", ", result.Scripts.Select(s => s.Name)));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed with exception");
            return false;
        }
    }

    public async Task<bool> HasPendingMigrationsAsync(CancellationToken ct = default)
    {
        try
        {
            var upgrader = DeployChanges
                .To
                .SqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(
                    Assembly.GetExecutingAssembly(),
                    s => s.StartsWith("Templates.Infrastructure.Persistence.Migrations.Scripts"))
                .Build();

            var scripts = upgrader.GetScriptsToExecute();
            return scripts.Any();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for pending migrations");
            return false;
        }
    }

    public async Task<List<string>> GetAppliedMigrationsAsync(CancellationToken ct = default)
    {
        try
        {
            var upgrader = DeployChanges
                .To
                .SqlDatabase(_connectionString)
                .Build();

            var executedScripts = upgrader.GetExecutedScripts();
            return executedScripts.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve applied migrations");
            return new();
        }
    }
}

/// <summary>
/// Extension methods for database migrations.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Add database migration service to dependency injection.
    /// </summary>
    public static IServiceCollection AddDatabaseMigrations(
        this IServiceCollection services)
    {
        services.AddScoped<IDatabaseMigrationService, DbUpMigrationService>();
        return services;
    }

    /// <summary>
    /// Run pending database migrations during application startup.
    /// Call this in Program.cs after building the app.
    /// </summary>
    public static async Task MigrateAsync(
        this WebApplication app,
        CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

        if (!await migrationService.MigrateAsync(ct))
        {
            throw new InvalidOperationException(
                "Database migration failed. Check logs for details. " +
                "Ensure database exists and connection string is correct.");
        }
    }
}
