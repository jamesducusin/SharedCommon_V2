using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SharedCommon.Configuration;

namespace SharedCommon.Core.UnitTests.Configuration;

/// <summary>
/// Unit tests for <see cref="EnterpriseConfigurationValidator"/>.
/// </summary>
public sealed class EnterpriseConfigurationValidatorTests
{
    private readonly IConfigurationBuilder _configBuilder;

    public EnterpriseConfigurationValidatorTests()
    {
        _configBuilder = new ConfigurationBuilder();
    }

    [Fact]
    public void ValidateAll_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "Cors:AllowedOrigins:0", "https://example.com" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "Observability:SamplingRate", "0.1" },
                { "RateLimit:Enabled", "true" },
                { "RateLimit:Limit", "100" },
                { "RateLimit:WindowSeconds", "60" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        validator.ValidateAll(); // Should not throw
    }

    [Fact]
    public void ValidateDatabaseConfiguration_WithMissingConnectionString_AddError()
    {
        // Arrange
        var config = _configBuilder.Build();
        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("DefaultConnection", ex.Message);
    }

    [Fact]
    public void ValidateDatabaseConfiguration_WithPlaintextPassword_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Password=secret123" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("plaintext password", ex.Message);
    }

    [Fact]
    public void ValidateRedisConfiguration_WithMissingConnectionString_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("Redis", ex.Message);
    }

    [Fact]
    public void ValidateRedisConfiguration_WithAlternativeConnectionStringKey_Succeeds()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "Redis:ConnectionString", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        validator.ValidateAll(); // Should not throw
    }

    [Fact]
    public void ValidateAuthenticationConfiguration_WithMissingJwtKey_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("JWT", ex.Message);
    }

    [Fact]
    public void ValidateAuthenticationConfiguration_WithShortJwtKey_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "short" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("32 characters", ex.Message);
    }

    [Fact]
    public void ValidateAuthenticationConfiguration_WithMissingIssuer_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("issuer", ex.Message);
    }

    [Fact]
    public void ValidateCorsConfiguration_WithWildcardInProduction_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "Cors:AllowedOrigins:0", "*" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("wildcard", ex.Message);
    }

    [Fact]
    public void ValidateCorsConfiguration_WithHttpInProduction_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "Cors:AllowedOrigins:0", "http://example.com" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("HTTPS", ex.Message);
    }

    [Fact]
    public void ValidateObservabilityConfiguration_WithMissingServiceName_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("service name", ex.Message);
    }

    [Fact]
    public void ValidateObservabilityConfiguration_WithInvalidSamplingRate_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "Observability:SamplingRate", "1.5" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("between 0 and 1", ex.Message);
    }

    [Fact]
    public void ValidateRateLimitingConfiguration_WithInvalidLimit_AddError()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "true" },
                { "RateLimit:Limit", "0" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        Assert.Contains("positive integer", ex.Message);
    }

    [Fact]
    public void ValidateRateLimitingConfiguration_WithRateLimitDisabled_SkipsValidation()
    {
        // Arrange
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert - Should not throw even though Limit is missing
        validator.ValidateAll();
    }

    [Fact]
    public void ValidateAll_WithMultipleErrors_ReturnsAllInMessage()
    {
        // Arrange
        var config = _configBuilder.Build();
        var validator = new EnterpriseConfigurationValidator(config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAll());
        
        // Should contain multiple errors
        Assert.Contains("error", ex.Message);
        Assert.True(ex.Message.Contains("1.") && ex.Message.Contains("2."), 
            "Should format multiple errors with numbering");
    }

    [Fact]
    public void AddEnterpriseConfigurationValidation_RegistersValidatorInDI()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = _configBuilder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test" },
                { "ConnectionStrings:Redis", "localhost:6379" },
                { "Jwt:Key", "this-is-a-very-long-secret-key-of-at-least-32-chars" },
                { "Jwt:Issuer", "https://example.com" },
                { "Jwt:Audience", "api" },
                { "Observability:ServiceName", "MyService" },
                { "Observability:Environment", "prod" },
                { "RateLimit:Enabled", "false" }
            })
            .Build();

        // Act
        services.AddEnterpriseConfigurationValidation(config);
        var sp = services.BuildServiceProvider();

        // Assert
        var validator = sp.GetRequiredService<IEnterpriseConfigurationValidator>();
        Assert.NotNull(validator);
    }
}
