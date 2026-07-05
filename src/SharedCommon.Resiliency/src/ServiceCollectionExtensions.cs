using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

namespace SharedCommon.Resiliency;

/// <summary>DI registration extensions for SharedCommon resilience infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Polly resilience pipelines and <see cref="IResiliencyPolicyProvider"/>.
    /// Configuration is read from <c>SharedCommon:Resiliency</c>.
    /// </summary>
    public static IServiceCollection AddSharedResiliency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ResiliencyOptions>()
            .BindConfiguration(ResiliencyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ResiliencePipelineRegistry<string>>();
        services.AddSingleton<IResiliencyPolicyProvider, ResiliencyPolicyProvider>();

        return services;
    }
}
