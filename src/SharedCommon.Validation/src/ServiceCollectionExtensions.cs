using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace SharedCommon.Validation;

/// <summary>DI registration extensions for SharedCommon.Validation.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon validation services to the DI container.
    ///
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="ValidationOptions"/> — validated at startup.</item>
    ///   <item>All <see cref="IValidator{T}"/> implementations found in <paramref name="assembliesToScan"/>.</item>
    ///   <item><see cref="AutoValidationFilter"/> as a global MVC filter when
    ///         <see cref="ValidationOptions.AutomaticControllerValidation"/> is <c>true</c>.</item>
    /// </list>
    ///
    /// Example:
    /// <code>
    /// builder.Services.AddSharedCommonValidation(
    ///     builder.Configuration,
    ///     typeof(Program).Assembly);
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <param name="assembliesToScan">Assemblies to scan for <see cref="IValidator{T}"/> implementations.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddSharedCommonValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assembliesToScan)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ValidationOptions>()
            .BindConfiguration(ValidationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var opts = new ValidationOptions();
        configuration.GetSection(ValidationOptions.SectionName).Bind(opts);

        if (!opts.Enabled) return services;

        // Register all IValidator<T> implementations found in the supplied assemblies.
        if (assembliesToScan.Length > 0)
            services.AddValidatorsFromAssemblies(assembliesToScan, ServiceLifetime.Scoped);

        // Register the global auto-validation filter.
        if (opts.AutomaticControllerValidation)
        {
            services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(mvcOpts =>
            {
                mvcOpts.Filters.Add<AutoValidationFilter>();
            });
        }

        return services;
    }
}
