namespace Templates.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Validates required configuration at application startup.
/// Fails fast with clear error messages instead of runtime errors.
/// </summary>
public class ConfigurationValidator
{
    private readonly IConfiguration _config;
    private readonly List<string> _errors = new();

    public ConfigurationValidator(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Validate all required configuration sections and keys.
    /// </summary>
    public void ValidateAll()
    {
        ValidateConnectionStrings();
        ValidateJwtConfiguration();
        ValidateCorsConfiguration();
        ValidateObservabilityConfiguration();
        ValidateCacheConfiguration();
        ValidateMessageQueueConfiguration();

        if (_errors.Any())
        {
            var errorMessage = $"Configuration validation failed:\n{string.Join("\n", _errors.Select(e => $"  ❌ {e}"))}";
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Require specific configuration values to be present and non-empty.
    /// </summary>
    public void RequireValues(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _config[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                _errors.Add($"Missing or empty configuration: '{key}'");
            }
        }
    }

    /// <summary>
    /// Require a configuration value to match a specific pattern.
    /// </summary>
    public void RequirePattern(string key, string pattern, string description)
    {
        var value = _config[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            _errors.Add($"Missing configuration: '{key}'");
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
        {
            _errors.Add($"Configuration '{key}' does not match pattern: {description}. Got: {value}");
        }
    }

    /// <summary>
    /// Require connection string to be valid.
    /// </summary>
    public void RequireConnectionString(string connectionStringName)
    {
        var connectionString = _config.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _errors.Add($"Missing connection string: 'ConnectionStrings:{connectionStringName}'");
        }
    }

    private void ValidateConnectionStrings()
    {
        RequireConnectionString("DefaultConnection");
        
        // Validate connection string format
        var connStr = _config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connStr) && !connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            _errors.Add("DefaultConnection must specify 'Server' parameter");
        }
    }

    private void ValidateJwtConfiguration()
    {
        RequireValues(
            "Jwt:Key",
            "Jwt:Issuer",
            "Jwt:Audience",
            "Jwt:ExpirationMinutes");

        var jwtKey = _config["Jwt:Key"];
        if (!string.IsNullOrEmpty(jwtKey) && jwtKey.Length < 32)
        {
            _errors.Add("Jwt:Key must be at least 32 characters for security");
        }

        if (int.TryParse(_config["Jwt:ExpirationMinutes"], out var expiration) && expiration <= 0)
        {
            _errors.Add("Jwt:ExpirationMinutes must be greater than 0");
        }
    }

    private void ValidateCorsConfiguration()
    {
        RequireValues("Auth:AllowedOrigins");

        var origins = _config["Auth:AllowedOrigins"];
        if (!string.IsNullOrEmpty(origins) && origins == "*")
        {
            _errors.Add("Auth:AllowedOrigins cannot be '*' - specify explicit origins (security risk)");
        }
    }

    private void ValidateObservabilityConfiguration()
    {
        RequireValues(
            "Otel:ServiceName",
            "Otel:Environment",
            "Otel:ServiceVersion");

        RequirePattern(
            "Otel:SamplingRate",
            @"^(0(\.\d+)?|1(\.0+)?)$",
            "Must be decimal between 0 and 1");
    }

    private void ValidateCacheConfiguration()
    {
        RequireValues("Redis:ConnectionString");

        var redisConn = _config["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(redisConn) && !redisConn.Contains(":", StringComparison.Ordinal))
        {
            _errors.Add("Redis:ConnectionString must include port (format: host:port)");
        }
    }

    private void ValidateMessageQueueConfiguration()
    {
        RequireValues(
            "MessageQueue:Host",
            "MessageQueue:Username",
            "MessageQueue:Password");

        var username = _config["MessageQueue:Username"];
        if (!string.IsNullOrEmpty(username) && username == "guest")
        {
            _errors.Add("MessageQueue:Username cannot be 'guest' in production");
        }
    }
}

/// <summary>
/// Configuration validation extension for IServiceCollection.
/// </summary>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Add configuration validation to the service collection.
    /// Validates on application startup.
    /// </summary>
    public static IServiceCollection AddConfigurationValidation(
        this IServiceCollection services,
        IConfiguration config)
    {
        var validator = new ConfigurationValidator(config);
        validator.ValidateAll();

        return services;
    }

    /// <summary>
    /// Log validated configuration keys (redacts sensitive values).
    /// </summary>
    public static IServiceCollection LogValidatedConfiguration(
        this IServiceCollection services,
        IConfiguration config,
        ILogger logger)
    {
        var redactedKeys = new[] { "Key", "Password", "Secret", "Token", "ApiKey" };

        logger.LogInformation("✓ Configuration validated:");
        logger.LogInformation("  Jwt:Issuer = {Issuer}", config["Jwt:Issuer"]);
        logger.LogInformation("  Otel:Environment = {Environment}", config["Otel:Environment"]);
        logger.LogInformation("  Otel:SamplingRate = {SamplingRate}", config["Otel:SamplingRate"]);
        logger.LogInformation("  Redis:ConnectionString = {Redis}", 
            SanitizeConnectionString(config["Redis:ConnectionString"]));

        return services;
    }

    private static string SanitizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "(not configured)";

        // Hide password
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(password|pwd|key)=([^;]+)",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
