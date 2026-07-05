using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Cloud.Azure;

/// <summary>DI registration for SharedCommon.Cloud.Azure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure implementations of the SharedCommon.Cloud abstractions.
    /// Call <c>AddSharedCloud(configuration)</c> first to bind <see cref="CloudOptions"/>.
    ///
    /// <code>
    /// builder.Services
    ///     .AddSharedCloud(builder.Configuration)
    ///     .AddSharedCloudAzure();
    /// </code>
    /// </summary>
    public static IServiceCollection AddSharedCloudAzure(this IServiceCollection services)
    {
        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        services.AddSingleton<ISecretManagerService, AzureSecretManagerService>();
        services.AddSingleton<ICloudQueueService, AzureServiceBusQueueService>();
        return services;
    }
}
