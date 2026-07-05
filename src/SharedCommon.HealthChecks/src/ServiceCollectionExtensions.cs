using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedCommon.HealthChecks.Checks;

namespace SharedCommon.HealthChecks;

/// <summary>DI registration extensions for SharedCommon health checks.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ASP.NET Core health check system with SharedCommon standard checks.
    /// Checks are configured from <c>SharedCommon:HealthChecks</c> in <paramref name="configuration"/>.
    /// </summary>
    public static IServiceCollection AddSharedHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<HealthCheckOptions>()
            .BindConfiguration(HealthCheckOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IHealthCheckReporter, HealthCheckReporter>();

        var options = configuration
            .GetSection(HealthCheckOptions.SectionName)
            .Get<HealthCheckOptions>() ?? new HealthCheckOptions();

        var builder = services.AddHealthChecks();

        // Liveness tag: fast, no external calls — used by /health/live
        builder.AddCheck("liveness", () => HealthCheckResult.Healthy(), tags: ["live"]);

        if (options.Redis?.Enabled == true)
        {
            builder.AddCheck<RedisHealthCheck>(
                options.Redis.Name,
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "redis"],
                timeout: options.DefaultTimeout);
        }

        foreach (var http in options.ExternalHttp)
        {
            if (string.IsNullOrWhiteSpace(http.Name) || string.IsNullOrWhiteSpace(http.Uri))
                continue;

            services.AddHttpClient(http.Name, client =>
                client.BaseAddress = new Uri(http.Uri));

            builder.AddCheck(
                http.Name,
                new ExternalHttpHealthCheck(
                    services.BuildServiceProvider()
                        .GetRequiredService<IHttpClientFactory>()
                        .CreateClient(http.Name)),
                tags: ["ready", "external"],
                timeout: options.DefaultTimeout);
        }

        return services;
    }
}
