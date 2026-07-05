using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Storage;

/// <summary>DI registration for SharedCommon.Storage.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IFileStorageService"/> with the configured provider.
    ///
    /// <para>
    /// Currently supported providers: <see cref="StorageProvider.Local"/> (default).
    /// Cloud provider requires <c>SharedCommon.Cloud</c> installed and configured.
    /// </para>
    ///
    /// <code>
    /// // Program.cs
    /// builder.Services.AddSharedStorage(builder.Configuration);
    /// </code>
    /// </summary>
    public static IServiceCollection AddSharedStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
