using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Auditing;

/// <summary>DI registration extensions for SharedCommon auditing infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the audit service and configures the storage backend.
    ///
    /// Default backend is <see cref="AuditStorageBackend.Logging"/> — structured log output,
    /// no database required. Switch via configuration:
    ///
    /// <code>
    /// {
    ///   "SharedCommon": {
    ///     "Auditing": {
    ///       "Backend": "Logging",
    ///       "CaptureValueSnapshots": true,
    ///       "RetentionDays": 90
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// Usage:
    /// <code>
    /// builder.Services.AddSharedAuditing(builder.Configuration);
    ///
    /// // Then inject IAuditService anywhere:
    /// // public class OrderService(IAuditService audit) { ... }
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    public static IServiceCollection AddSharedAuditing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuditOptions>()
            .BindConfiguration(AuditOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(AuditOptions.SectionName)
            .Get<AuditOptions>() ?? new AuditOptions();

        switch (options.Backend)
        {
            case AuditStorageBackend.Database:
                // Register the database-backed implementation.
                // Consumers must register AuditDbContext separately.
                services.AddScoped<IAuditService, DatabaseAuditService>();
                break;

            default:
                services.AddScoped<IAuditService, LoggingAuditService>();
                break;
        }

        return services;
    }
}
