namespace Templates.Api.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Templates.Api.Infrastructure.HealthChecks;

/// <summary>
/// Extensions for registering API layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Templates API",
                Version = "v1",
                Description = "Cloud-ready DDD API with Clean Architecture",
                Contact = new OpenApiContact
                {
                    Name = "Your Team",
                    Email = "support@example.com"
                }
            });

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        // Add CORS (Environment-aware and secure)
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                var allowedMethods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>() 
                    ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
                var allowedHeaders = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() 
                    ?? new[] { "Content-Type", "Authorization" };
                var allowCredentials = configuration.GetValue<bool>("Cors:AllowCredentials");

                if (allowedOrigins == null || allowedOrigins.Length == 0)
                {
                    // Safe default: only localhost for development
                    if (builder.HttpContext?.RequestServices.GetService(typeof(IHostEnvironment)) 
                        is IHostEnvironment hostEnv && hostEnv.IsDevelopment())
                    {
                        allowedOrigins = new[] { "http://localhost:3000", "http://localhost:5173" };
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "CORS AllowedOrigins must be configured in appsettings.json. " +
                            "See Cors:AllowedOrigins configuration key.");
                    }
                }

                builder
                    .WithOrigins(allowedOrigins)
                    .WithMethods(allowedMethods)
                    .WithHeaders(allowedHeaders);

                // Only allow credentials if explicitly configured (security best practice)
                if (allowCredentials)
                    builder.AllowCredentials();
            });
        });

        // Add health check service
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        return services;
    }
}
