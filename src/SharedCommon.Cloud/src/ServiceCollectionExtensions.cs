using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Cloud;

/// <summary>DI registration for SharedCommon.Cloud.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers cloud provider abstractions (<see cref="IBlobStorageService"/>,
    /// <see cref="ISecretManagerService"/>, <see cref="ICloudQueueService"/>).
    ///
    /// The active provider is selected via <c>SharedCommon:Cloud:Provider</c>.
    /// Implementations are registered by installing the matching provider package
    /// (e.g., SharedCommon.Cloud.Azure or SharedCommon.Cloud.Aws) and calling the
    /// provider-specific registration method before or after this call.
    ///
    /// <code>
    /// builder.Services.AddSharedCloud(builder.Configuration);
    /// </code>
    /// </summary>
    public static IServiceCollection AddSharedCloud(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CloudOptions>()
            .Bind(configuration.GetSection(CloudOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
