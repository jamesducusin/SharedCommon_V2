using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Security;

/// <summary>DI registration extensions for SharedCommon.Security.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon security services to the DI container.
    ///
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="SecurityOptions"/> — validated at startup.</item>
    ///   <item><see cref="IRateLimitService"/> / <see cref="InMemoryRateLimitService"/> — singleton.</item>
    ///   <item><see cref="IInputValidator"/> / <see cref="InputValidator"/> — singleton.</item>
    ///   <item>CORS policy derived from <see cref="CorsOptions"/>.</item>
    /// </list>
    ///
    /// Middleware must be wired separately via <see cref="ApplicationBuilderExtensions"/>.
    ///
    /// Example:
    /// <code>
    /// builder.Services.AddSharedCommonSecurity(builder.Configuration);
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public static IServiceCollection AddSharedCommonSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SecurityOptions>()
            .BindConfiguration(SecurityOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IRateLimitService, InMemoryRateLimitService>();
        services.AddSingleton<IInputValidator, InputValidator>();

        services.AddCors(corsOpts =>
        {
            var section = configuration.GetSection($"{SecurityOptions.SectionName}:Cors");
            var corsConfig = new CorsOptions();
            section.Bind(corsConfig);

            if (!corsConfig.Enabled) return;

            corsOpts.AddDefaultPolicy(policy =>
            {
                if (corsConfig.AllowedOrigins.Length > 0)
                    policy.WithOrigins(corsConfig.AllowedOrigins);
                else
                    policy.AllowAnyOrigin();

                if (corsConfig.AllowedHeaders is ["*"])
                    policy.AllowAnyHeader();
                else
                    policy.WithHeaders(corsConfig.AllowedHeaders);

                if (corsConfig.AllowedMethods.Length > 0)
                    policy.WithMethods(corsConfig.AllowedMethods);
                else
                    policy.AllowAnyMethod();

                if (corsConfig.AllowCredentials)
                    policy.AllowCredentials();

                policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.MaxAge));
            });
        });

        return services;
    }
}
