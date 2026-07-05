using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Middlewares;

/// <summary>DI registration extensions for SharedCommon.Middlewares.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon pipeline middleware services to the DI container.
    ///
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="MiddlewareOptions"/> — validated at startup.</item>
    /// </list>
    ///
    /// The actual middleware must be added to the pipeline via
    /// <see cref="ApplicationBuilderExtensions"/>:
    /// <code>
    /// builder.Services.AddSharedCommonMiddlewares(builder.Configuration);
    ///
    /// // In pipeline:
    /// app.UseSharedCommonExceptionHandling();
    /// app.UseSharedCommonCorrelationId();
    /// app.UseSharedCommonRequestLogging();
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public static IServiceCollection AddSharedCommonMiddlewares(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MiddlewareOptions>()
            .BindConfiguration(MiddlewareOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
