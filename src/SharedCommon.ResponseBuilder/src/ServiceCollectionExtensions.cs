using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.ResponseBuilder;

/// <summary>DI registration extensions for SharedCommon.ResponseBuilder.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IResponseBuilder"/> as a scoped service so the correlation ID
    /// from the current request is automatically injected into all responses.
    /// </summary>
    public static IServiceCollection AddSharedResponseBuilder(this IServiceCollection services)
    {
        services.AddScoped<IResponseBuilder, ResponseBuilder>();
        return services;
    }
}
