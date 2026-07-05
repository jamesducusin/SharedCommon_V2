using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedCommon.Configuration;

/// <summary>
/// Enterprise configuration validation service for startup.
/// Validates all critical dependencies and configuration before the application starts.
/// Fails fast with clear error messages if configuration is invalid.
/// </summary>
/// <remarks>
/// Usage in Program.cs:
/// <code>
/// var builder = WebApplicationBuilder.CreateBuilder(args);
/// builder.Services.AddEnterpriseConfigurationValidation(builder.Configuration);
/// 
/// var app = builder.Build();
/// app.Services.ValidateStartupConfiguration();  // Throws if invalid
/// </code>
/// </remarks>
public static class EnterpriseConfigurationExtensions
{
    /// <summary>
    /// Registers enterprise configuration validation in the DI container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEnterpriseConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IEnterpriseConfigurationValidator>(
            new EnterpriseConfigurationValidator(configuration));

        return services;
    }

    /// <summary>
    /// Validates all startup configuration (called after DI container is built).
    /// Throws an exception if validation fails.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    public static void ValidateStartupConfiguration(this IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<EnterpriseConfigurationValidator>>();
        var validator = serviceProvider.GetRequiredService<IEnterpriseConfigurationValidator>();
        
        logger.LogInformation("Validating startup configuration...");
        validator.ValidateAll();
        logger.LogInformation("Startup configuration validation completed successfully");
    }
}

/// <summary>
/// Validates enterprise configuration requirements.
/// </summary>
public interface IEnterpriseConfigurationValidator
{
    /// <summary>
    /// Validates all required configuration. Throws if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if configuration is invalid</exception>
    void ValidateAll();

    /// <summary>
    /// Validates Redis connection configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if Redis config is invalid</exception>
    void ValidateRedisConfiguration();

    /// <summary>
    /// Validates database connection configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if database config is invalid</exception>
    void ValidateDatabaseConfiguration();

    /// <summary>
    /// Validates JWT/authentication configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if auth config is invalid</exception>
    void ValidateAuthenticationConfiguration();

    /// <summary>
    /// Validates CORS configuration for production safety.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if CORS config violates security rules</exception>
    void ValidateCorsConfiguration();

    /// <summary>
    /// Validates OpenTelemetry configuration for observability.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if OTEL config is invalid</exception>
    void ValidateObservabilityConfiguration();

    /// <summary>
    /// Validates rate limiting configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if rate limit config is invalid</exception>
    void ValidateRateLimitingConfiguration();
}

/// <summary>
/// Default implementation of <see cref="IEnterpriseConfigurationValidator"/>.
/// </summary>
public sealed class EnterpriseConfigurationValidator : IEnterpriseConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly List<string> _errors = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnterpriseConfigurationValidator"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    public EnterpriseConfigurationValidator(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public void ValidateAll()
    {
        _errors.Clear();

        ValidateDatabaseConfiguration();
        ValidateRedisConfiguration();
        ValidateAuthenticationConfiguration();
        ValidateCorsConfiguration();
        ValidateObservabilityConfiguration();
        ValidateRateLimitingConfiguration();

        if (_errors.Count > 0)
        {
            var message = $"Configuration validation failed with {_errors.Count} error(s):\n" +
                string.Join("\n", _errors.Select((e, i) => $"  {i + 1}. {e}"));
            throw new InvalidOperationException(message);
        }
    }

    /// <inheritdoc />
    public void ValidateDatabaseConfiguration()
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connStr))
            _errors.Add("Database connection string 'DefaultConnection' is not configured");
        
        if (!string.IsNullOrEmpty(connStr) && connStr.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            if (connStr.Contains("password=", StringComparison.OrdinalIgnoreCase))
                _errors.Add("Database connection string should not contain plaintext password. Use Azure Key Vault or environment variables.");
        }
    }

    /// <inheritdoc />
    public void ValidateRedisConfiguration()
    {
        var connStr = _configuration.GetConnectionString("Redis") ?? 
                     _configuration["Redis:ConnectionString"] ?? 
                     _configuration["CacheOptions:ConnectionString"];
        
        if (string.IsNullOrEmpty(connStr))
            _errors.Add("Redis connection string not configured. Required for caching, rate limiting, and distributed feature flags.");
    }

    /// <inheritdoc />
    public void ValidateAuthenticationConfiguration()
    {
        var jwtKey = _configuration["Jwt:Key"] ?? _configuration["Authentication:JwtKey"];
        
        if (string.IsNullOrEmpty(jwtKey))
            _errors.Add("JWT signing key is not configured. Set 'Jwt:Key' in configuration.");
        else if (jwtKey.Length < 32)
            _errors.Add("JWT signing key must be at least 32 characters for HS256 security.");

        var issuer = _configuration["Jwt:Issuer"] ?? _configuration["Authentication:JwtIssuer"];
        if (string.IsNullOrEmpty(issuer))
            _errors.Add("JWT issuer is not configured. Set 'Jwt:Issuer' in configuration.");

        var audience = _configuration["Jwt:Audience"] ?? _configuration["Authentication:JwtAudience"];
        if (string.IsNullOrEmpty(audience))
            _errors.Add("JWT audience is not configured. Set 'Jwt:Audience' in configuration.");
    }

    /// <inheritdoc />
    public void ValidateCorsConfiguration()
    {
        var corsPolicy = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

        if (corsPolicy != null && environment == "Production")
        {
            if (corsPolicy.Contains("*"))
                _errors.Add("CORS wildcard '*' is not allowed in production. Specify exact origins.");
            
            if (corsPolicy.Any(o => o.Contains("http://", StringComparison.OrdinalIgnoreCase)))
                _errors.Add("CORS should not allow plain HTTP origins in production. Use HTTPS only.");
        }
    }

    /// <inheritdoc />
    public void ValidateObservabilityConfiguration()
    {
        var serviceName = _configuration["Observability:ServiceName"] ?? 
                         _configuration["OpenTelemetry:ServiceName"];
        
        if (string.IsNullOrEmpty(serviceName))
            _errors.Add("Observability service name is not configured. Set 'Observability:ServiceName' for telemetry tracing.");

        var environment = _configuration["Observability:Environment"] ?? 
                         _configuration["OpenTelemetry:Environment"];
        
        if (string.IsNullOrEmpty(environment))
            _errors.Add("Observability environment is not configured. Set 'Observability:Environment' (dev/staging/prod).");

        var samplingRateStr = _configuration["Observability:SamplingRate"] ?? 
                             _configuration["OpenTelemetry:SamplingRate"] ?? 
                             "0.1";
        
        if (double.TryParse(samplingRateStr, out var samplingRate))
        {
            if (samplingRate < 0 || samplingRate > 1)
                _errors.Add("Observability sampling rate must be between 0 and 1 (0 = none, 1 = all).");
        }
        else
        {
            _errors.Add($"Observability sampling rate '{samplingRateStr}' is not a valid number.");
        }
    }

    /// <inheritdoc />
    public void ValidateRateLimitingConfiguration()
    {
        var enabledStr = _configuration["RateLimit:Enabled"] ?? "true";
        
        if (bool.TryParse(enabledStr, out var enabled) && enabled)
        {
            var limitStr = _configuration["RateLimit:Limit"] ?? "100";
            if (!int.TryParse(limitStr, out var limit) || limit <= 0)
                _errors.Add("Rate limit 'Limit' must be a positive integer.");

            var windowStr = _configuration["RateLimit:WindowSeconds"] ?? "60";
            if (!int.TryParse(windowStr, out var window) || window <= 0)
                _errors.Add("Rate limit 'WindowSeconds' must be a positive integer.");
        }
    }
}
