using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.MultiTenancy;

/// <summary>DI registration for SharedCommon.MultiTenancy.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers multi-tenancy infrastructure: <see cref="ITenantContext"/>,
    /// <see cref="ITenantResolver"/>, and the tenant middleware.
    ///
    /// <para>
    /// After calling this, add <c>app.UseSharedMultiTenancy()</c> in Program.cs before
    /// <c>app.UseAuthentication()</c> / <c>app.UseAuthorization()</c>.
    /// </para>
    ///
    /// <code>
    /// // Program.cs
    /// builder.Services.AddSharedMultiTenancy(builder.Configuration);
    /// // ...
    /// app.UseSharedMultiTenancy();
    /// </code>
    /// </summary>
    public static IServiceCollection AddSharedMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MultiTenancyOptions>()
            .Bind(configuration.GetSection(MultiTenancyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantResolver, DefaultTenantResolver>();

        return services;
    }
}
