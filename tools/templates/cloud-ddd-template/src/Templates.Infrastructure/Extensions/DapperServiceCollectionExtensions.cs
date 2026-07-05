using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Infrastructure.Persistence.Dapper;

namespace Templates.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Dapper persistence services.
/// Provides convenient DI registration for high-performance data access.
/// </summary>
public static class DapperServiceCollectionExtensions
{
    /// <summary>
    /// Register Dapper repositories and services for dependency injection.
    /// Configures connection pooling and stored procedure execution.
    /// </summary>
    /// <param name="services">Service collection to register services into</param>
    /// <param name="configuration">Application configuration containing connection string</param>
    /// <returns>Service collection for chaining</returns>
    /// <example>
    /// Usage in Program.cs:
    /// <code>
    /// builder.Services.AddDapperPersistence(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddDapperPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration");

        // Register the database connection factory
        services.AddScoped<IDbConnection>(sp =>
            new SqlConnection(connectionString));

        // Register the stored procedure executor
        services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

        // Register generic Dapper repository factory
        services.AddScoped(typeof(IDapperRepository<,>), typeof(DapperRepository<,>));

        return services;
    }

    /// <summary>
    /// Register Dapper repositories with a specific connection string.
    /// Use this overload if you have multiple databases or need custom connection handling.
    /// </summary>
    /// <param name="services">Service collection to register services into</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDapperPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        services.AddScoped<IDbConnection>(sp =>
            new SqlConnection(connectionString));

        services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

        services.AddScoped(typeof(IDapperRepository<,>), typeof(DapperRepository<,>));

        return services;
    }

    /// <summary>
    /// Register Dapper repositories with advanced configuration options.
    /// Use this for fine-grained control over connection pooling and timeouts.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration delegate</param>
    /// <returns>Service collection for chaining</returns>
    /// <example>
    /// <code>
    /// services.AddDapperPersistenceWithConfiguration(config =>
    /// {
    ///     config.ConnectionString = "Server=.;Database=MyDb;...";
    ///     config.CommandTimeout = 60;
    ///     config.MaxPoolSize = 100;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddDapperPersistenceWithConfiguration(
        this IServiceCollection services,
        Action<DapperConfiguration> configuration)
    {
        var config = new DapperConfiguration();
        configuration(config);

        if (string.IsNullOrWhiteSpace(config.ConnectionString))
            throw new InvalidOperationException("Connection string must be specified in DapperConfiguration");

        services.AddScoped<IDbConnection>(sp =>
            new SqlConnection(config.ConnectionString));

        services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

        services.AddScoped(typeof(IDapperRepository<,>), typeof(DapperRepository<,>));

        return services;
    }
}

/// <summary>
/// Configuration options for Dapper persistence layer.
/// </summary>
public class DapperConfiguration
{
    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command timeout in seconds (default: 30).
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum connection pool size (default: 100).
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable detailed logging of SQL commands.
    /// </summary>
    public bool LogSqlCommands { get; set; } = false;
}
