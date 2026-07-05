using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Core;

/// <summary>DI registration extensions for SharedCommon.Core.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon core services to the DI container.
    ///
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="CoreOptions"/> — validated at startup.</item>
    ///   <item><see cref="IRequestContext"/> / <see cref="RequestContext"/> — scoped per request.</item>
    /// </list>
    ///
    /// Example:
    /// <code>
    /// builder.Services.AddSharedCommonCore(builder.Configuration);
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddSharedCommonCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CoreOptions>()
            .BindConfiguration(CoreOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                opts => opts.EnvironmentName is "Development" or "Staging" or "Production",
                "EnvironmentName must be one of: Development, Staging, Production.")
            .ValidateOnStart();

        services.AddScoped<IRequestContext, RequestContext>();

        return services;
    }
}
