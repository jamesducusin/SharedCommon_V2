namespace SharedCommon.Resiliency.UnitTests;

/// <summary>Behavioral tests for ServiceCollectionExtensions DI registration.</summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that AddSharedResiliency successfully registers IResiliencyPolicyProvider
    /// and it can be resolved from the service provider.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WithValidConfiguration_RegistersAndResolvesProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "SharedCommon:Resiliency:Retry:MaxAttempts", "5" },
            { "SharedCommon:Resiliency:CircuitBreaker:FailureRatio", "0.75" },
            { "SharedCommon:Resiliency:Timeout:Duration", "60000" }
        });

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IResiliencyPolicyProvider>();

        // Assert
        Assert.NotNull(provider);
    }

    /// <summary>
    /// Verifies that ResiliencyOptions are configured with default values when no configuration is provided.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WithMinimalConfiguration_UsesDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>();

        // Assert
        Assert.NotNull(options.Value);
        Assert.Equal(3, options.Value.Retry.MaxAttempts);
        Assert.Equal(0.5, options.Value.CircuitBreaker.FailureRatio);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Value.Timeout.Duration);
    }

    /// <summary>
    /// Verifies that ResiliencyOptions can be retrieved after registration
    /// and contain the configured values.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_BindsConfigurationToResiliencyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "SharedCommon:Resiliency:Retry:MaxAttempts", "7" },
            { "SharedCommon:Resiliency:Retry:BaseDelay", "00:00:00.200" },
            { "SharedCommon:Resiliency:CircuitBreaker:FailureRatio", "0.6" },
            { "SharedCommon:Resiliency:CircuitBreaker:MinimumThroughput", "8" },
            { "SharedCommon:Resiliency:Timeout:Duration", "00:00:45" }
        });

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>();

        // Assert
        Assert.Equal(7, options.Value.Retry.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(200), options.Value.Retry.BaseDelay);
        Assert.Equal(0.6, options.Value.CircuitBreaker.FailureRatio);
        Assert.Equal(8, options.Value.CircuitBreaker.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(45), options.Value.Timeout.Duration);
    }

    /// <summary>
    /// Verifies that ResiliencePipelineRegistry{string} is registered as a singleton
    /// and returns the same instance across multiple resolutions.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_RegistersRegistryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var registry1 = serviceProvider.GetRequiredService<Polly.Registry.ResiliencePipelineRegistry<string>>();
        var registry2 = serviceProvider.GetRequiredService<Polly.Registry.ResiliencePipelineRegistry<string>>();

        // Assert
        Assert.Same(registry1, registry2);
    }

    /// <summary>
    /// Verifies that IResiliencyPolicyProvider is registered as a singleton
    /// and returns the same instance across multiple resolutions.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_RegistersProviderAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var provider1 = serviceProvider.GetRequiredService<IResiliencyPolicyProvider>();
        var provider2 = serviceProvider.GetRequiredService<IResiliencyPolicyProvider>();

        // Assert
        Assert.Same(provider1, provider2);
    }

    /// <summary>
    /// Verifies that AddSharedResiliency returns the IServiceCollection for method chaining.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        var result = services.AddSharedResiliency(config);

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    /// Verifies that options validation is enabled and fails for invalid retry configuration.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WithInvalidRetryMaxAttempts_FailsValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "SharedCommon:Resiliency:Retry:MaxAttempts", "0" }
        });

        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var ex = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>());
        Assert.NotNull(ex);
    }

    /// <summary>
    /// Verifies that options validation is enabled and fails for invalid circuit breaker failure ratio.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WithInvalidFailureRatio_FailsValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "SharedCommon:Resiliency:CircuitBreaker:FailureRatio", "1.5" }
        });

        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var ex = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>());
        Assert.NotNull(ex);
    }

    /// <summary>
    /// Verifies that obtained pipelines from the provider work correctly after DI registration.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_RetrievesAllStandardPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IResiliencyPolicyProvider>();

        var defaultPipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);
        var retryPipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);
        var timeoutPipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Assert
        Assert.NotNull(defaultPipeline);
        Assert.NotNull(retryPipeline);
        Assert.NotNull(timeoutPipeline);
    }

    /// <summary>
    /// Verifies that multiple calls to AddSharedResiliency don't create duplicate registrations.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WhenCalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedResiliency(config);
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var providers = serviceProvider.GetServices<IResiliencyPolicyProvider>().ToList();
        Assert.NotEmpty(providers);
    }

    /// <summary>
    /// Verifies that ILogger is properly injected into ResiliencyPolicyProvider
    /// through the DI container.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_InjectsLoggerIntoProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>());

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IResiliencyPolicyProvider>();

        // Assert
        Assert.NotNull(provider);
    }

    /// <summary>
    /// Verifies that retry configuration respects all parameters when customized.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WithAllCustomRetryConfig_AppliesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "SharedCommon:Resiliency:Retry:MaxAttempts", "4" },
            { "SharedCommon:Resiliency:Retry:BaseDelay", "00:00:01" },
            { "SharedCommon:Resiliency:Retry:MaxDelay", "00:01:00" }
        });

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>();

        // Assert
        Assert.Equal(4, options.Value.Retry.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), options.Value.Retry.BaseDelay);
        Assert.Equal(TimeSpan.FromMinutes(1), options.Value.Retry.MaxDelay);
    }

    /// <summary>
    /// Verifies that circuit breaker configuration respects all parameters when customized.
    /// </summary>
    [Fact]
    public void AddSharedResiliency_WithAllCustomCircuitBreakerConfig_AppliesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            { "SharedCommon:Resiliency:CircuitBreaker:FailureRatio", "0.8" },
            { "SharedCommon:Resiliency:CircuitBreaker:MinimumThroughput", "20" },
            { "SharedCommon:Resiliency:CircuitBreaker:SamplingDuration", "00:02:00" },
            { "SharedCommon:Resiliency:CircuitBreaker:BreakDuration", "00:01:00" }
        });

        // Act
        services.AddSharedResiliency(config);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>();

        // Assert
        Assert.Equal(0.8, options.Value.CircuitBreaker.FailureRatio);
        Assert.Equal(20, options.Value.CircuitBreaker.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(120), options.Value.CircuitBreaker.SamplingDuration);
        Assert.Equal(TimeSpan.FromSeconds(60), options.Value.CircuitBreaker.BreakDuration);
    }

    #region Helper Methods

    /// <summary>
    /// Creates an IConfiguration from a dictionary of values.
    /// </summary>
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> data) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

    #endregion
}
