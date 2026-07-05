using Hangfire;
using Hangfire.InMemory;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.BackgroundJobs;

/// <summary>DI and pipeline registration for SharedCommon background job infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire with the configured storage backend and exposes
    /// <see cref="IBackgroundJobService"/> for scheduling jobs.
    ///
    /// Configuration is read from <c>SharedCommon:BackgroundJobs</c>:
    /// <code>
    /// {
    ///   "SharedCommon": {
    ///     "BackgroundJobs": {
    ///       "Backend": "SqlServer",
    ///       "ConnectionString": "...",
    ///       "WorkerCount": 10,
    ///       "EnableDashboard": true,
    ///       "DashboardRequiredRole": "admin"
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// Usage:
    /// <code>
    /// builder.Services.AddSharedBackgroundJobs(builder.Configuration);
    ///
    /// // In middleware pipeline:
    /// app.UseSharedBackgroundJobs(app.Configuration);
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    public static IServiceCollection AddSharedBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BackgroundJobOptions>()
            .BindConfiguration(BackgroundJobOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(BackgroundJobOptions.SectionName)
            .Get<BackgroundJobOptions>() ?? new BackgroundJobOptions();

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();

            switch (options.Backend)
            {
                case JobStorageBackend.SqlServer:
                    ArgumentException.ThrowIfNullOrEmpty(
                        options.ConnectionString,
                        "BackgroundJobOptions.ConnectionString is required when Backend is SqlServer.");
                    config.UseSqlServerStorage(options.ConnectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    });
                    break;

                default:
                    config.UseInMemoryStorage(new InMemoryStorageOptions
                    {
                        MaxExpirationTime = TimeSpan.FromHours(1)
                    });
                    break;
            }
        });

        services.AddHangfireServer(serverOptions =>
        {
            serverOptions.WorkerCount = options.WorkerCount;
            serverOptions.Queues = [options.DefaultQueue, "default"];
        });

        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        return services;
    }

    /// <summary>
    /// Mounts the Hangfire server and optionally the dashboard.
    /// Call this in the middleware pipeline after <c>UseAuthentication</c> and <c>UseAuthorization</c>.
    ///
    /// <code>
    /// app.UseSharedBackgroundJobs(app.Configuration);
    /// </code>
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IApplicationBuilder UseSharedBackgroundJobs(
        this WebApplication app,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(BackgroundJobOptions.SectionName)
            .Get<BackgroundJobOptions>() ?? new BackgroundJobOptions();

        if (options.EnableDashboard)
        {
            var dashboardOptions = new DashboardOptions
            {
                DashboardTitle = "Background Jobs",
                DisplayStorageConnectionString = false
            };

            if (!string.IsNullOrEmpty(options.DashboardRequiredRole))
            {
                dashboardOptions.Authorization =
                [
                    new RoleBasedDashboardAuthorizationFilter(options.DashboardRequiredRole)
                ];
            }

            app.UseHangfireDashboard(options.DashboardPath, dashboardOptions);
        }

        return app;
    }
}
