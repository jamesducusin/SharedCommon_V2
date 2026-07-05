namespace Templates.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Templates.Infrastructure.Extensions;
using Templates.Infrastructure.Persistence;
using Templates.Infrastructure.Persistence.Repositories;

/// <summary>
/// Extensions for registering infrastructure layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services to the dependency injection container.
    /// Registers Dapper for high-performance data access with stored procedures.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Dapper persistence layer (optimized for 1000+ TPS)
        services.AddDapperPersistence(configuration);

        // Register domain repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
