using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace SharedCommon.FeatureFlags;

/// <summary>DI registration for SharedCommon.FeatureFlags.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IFeatureFlagService"/> backed by Microsoft.FeatureManagement.
    ///
    /// Feature definitions are read from the standard <c>FeatureManagement</c> configuration
    /// section. SharedCommon overrides (logging, caching) are read from
    /// <c>SharedCommon:FeatureFlags</c>.
    ///
    /// <code>
    /// // Program.cs
    /// builder.Services.AddSharedFeatureFlags(builder.Configuration);
    ///
    /// // appsettings.json
    /// {
    ///   "FeatureManagement": {
    ///     "NewDashboard": true,
    ///     "BetaCheckout": false
    ///   }
    /// }
    /// </code>
    /// </summary>
    public static IServiceCollection AddSharedFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<FeatureFlagOptions>()
            .Bind(configuration.GetSection(FeatureFlagOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));

        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        return services;
    }
}
