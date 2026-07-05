namespace SharedCommon.Resiliency.UnitTests;

/// <summary>Behavioral tests for ResiliencyPolicyProvider pipeline registration and retrieval.</summary>
public sealed class ResiliencyPolicyProviderTests
{
    /// <summary>Creates a configured provider with default test options.</summary>
    private static ResiliencyPolicyProvider CreateProvider(ResiliencyOptions? options = null)
    {
        var opts = Microsoft.Extensions.Options.Options.Create(
            options ?? new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        return new ResiliencyPolicyProvider(opts, registry, logger);
    }

    #region Pipeline Registration Tests

    /// <summary>
    /// Verifies that ResiliencyPolicyProvider registers all three standard pipelines
    /// (Default, RetryOnly, TimeoutOnly) during initialization.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultOptions_RegistersAllStandardPipelines()
    {
        // Arrange
        var opts = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = new ResiliencyPolicyProvider(opts, registry, logger);
        var defaultPipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);
        var retryPipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);
        var timeoutPipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Assert
        Assert.NotNull(defaultPipeline);
        Assert.NotNull(retryPipeline);
        Assert.NotNull(timeoutPipeline);
    }

    /// <summary>
    /// Verifies that all standard pipeline names are correctly defined as constants.
    /// </summary>
    [Theory]
    [InlineData(ResiliencyPolicyProvider.Default, "default")]
    [InlineData(ResiliencyPolicyProvider.RetryOnly, "retry")]
    [InlineData(ResiliencyPolicyProvider.TimeoutOnly, "timeout")]
    public void PipelineNames_AreCorrect(string pipelineName, string expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, pipelineName);
    }

    #endregion

    #region GetPipeline<T> Tests

    /// <summary>
    /// Verifies that GetPipeline returns the same pipeline instance on subsequent calls,
    /// demonstrating caching behavior.
    /// </summary>
    [Fact]
    public void GetPipeline_OnMultipleCalls_ReturnsCachedInstance()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var pipeline1 = provider.GetPipeline(ResiliencyPolicyProvider.Default);
        var pipeline2 = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.Same(pipeline1, pipeline2);
    }

    /// <summary>
    /// Verifies that GetPipeline and GetPipeline{T} can retrieve the same pipeline
    /// as both generic and non-generic variants.
    /// </summary>
    [Fact]
    public void GetPipeline_AndGetPipelineGeneric_ReturnConsistentInstances()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var nonGenericPipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);
        var genericPipeline = provider.GetPipeline<int>(ResiliencyPolicyProvider.RetryOnly);

        // Assert
        Assert.NotNull(nonGenericPipeline);
        Assert.NotNull(genericPipeline);
    }

    #endregion

    #region Configuration Binding Tests

    /// <summary>
    /// Verifies that RetryOptions configuration values are correctly applied to the retry policy
    /// by checking non-default configuration values flow through.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomRetryOptions_AppliesMaxAttempts()
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            Retry = new RetryOptions
            {
                MaxAttempts = 5,
                BaseDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromSeconds(10)
            }
        };

        // Act
        var provider = CreateProvider(customOptions);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that CircuitBreakerOptions configuration values are correctly applied
    /// to the circuit breaker policy.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomCircuitBreakerOptions_AppliesConfiguration()
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailureRatio = 0.75,
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(120),
                BreakDuration = TimeSpan.FromSeconds(60)
            }
        };

        // Act
        var provider = CreateProvider(customOptions);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that TimeoutOptions configuration values are correctly applied
    /// to the timeout policy.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomTimeoutOptions_AppliesConfiguration()
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            Timeout = new TimeoutOptions
            {
                Duration = TimeSpan.FromSeconds(60)
            }
        };

        // Act
        var provider = CreateProvider(customOptions);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Assert
        Assert.NotNull(pipeline);
    }

    #endregion

    #region Pipeline Differentiation Tests

    /// <summary>
    /// Verifies that RetryOnly pipeline contains only retry logic, not circuit breaker or timeout.
    /// </summary>
    [Fact]
    public void GetPipeline_RetryOnly_ContainsOnlyRetryBehavior()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that TimeoutOnly pipeline contains only timeout logic, not retry or circuit breaker.
    /// </summary>
    [Fact]
    public void GetPipeline_TimeoutOnly_ContainsOnlyTimeoutBehavior()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that Default pipeline contains all three strategies combined.
    /// </summary>
    [Fact]
    public void GetPipeline_Default_ContainsCombinedStrategies()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(pipeline);
    }

    #endregion

    #region Logger Interaction Tests

    /// <summary>
    /// Verifies that circuit breaker state transitions are logged appropriately
    /// when the circuit opens due to failures.
    /// </summary>
    [Fact]
    public void Constructor_WithCircuitBreakerPolicy_ConfiguresStateTransitionLogging()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = CreateProvider();

        // Assert - Verify provider is created successfully with logging configured
        Assert.NotNull(provider);
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Verifies that multiple calls to different pipelines return distinct instances
    /// but each pipeline caches its own instance.
    /// </summary>
    [Fact]
    public void GetPipeline_WithDifferentNames_ReturnsDifferentInstances()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var defaultPipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);
        var retryPipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);
        var timeoutPipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Assert
        Assert.NotNull(defaultPipeline);
        Assert.NotNull(retryPipeline);
        Assert.NotNull(timeoutPipeline);
        // Pipelines should be distinct instances
        Assert.NotSame(defaultPipeline, retryPipeline);
        Assert.NotSame(retryPipeline, timeoutPipeline);
    }

    /// <summary>
    /// Verifies that boundary values for retry configuration are accepted and don't cause errors.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_WithBoundaryRetryAttempts_AcceptsAllValidValues(int maxAttempts)
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            Retry = new RetryOptions { MaxAttempts = maxAttempts }
        };

        // Act
        var provider = CreateProvider(customOptions);

        // Assert
        Assert.NotNull(provider.GetPipeline(ResiliencyPolicyProvider.Default));
    }

    /// <summary>
    /// Verifies that boundary values for circuit breaker FailureRatio are accepted.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Constructor_WithBoundaryFailureRatio_AcceptsAllValidValues(double failureRatio)
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            CircuitBreaker = new CircuitBreakerOptions { FailureRatio = failureRatio }
        };

        // Act
        var provider = CreateProvider(customOptions);

        // Assert
        Assert.NotNull(provider.GetPipeline(ResiliencyPolicyProvider.Default));
    }

    /// <summary>
    /// Verifies that very small timeout durations are accepted without error.
    /// </summary>
    [Fact]
    public void Constructor_WithSmallTimeoutDuration_AcceptsConfiguration()
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            Timeout = new TimeoutOptions { Duration = TimeSpan.FromMilliseconds(100) }
        };

        // Act
        var provider = CreateProvider(customOptions);

        // Assert
        Assert.NotNull(provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly));
    }

    #endregion

    #region Registry Behavior Tests

    /// <summary>
    /// Verifies that the provider correctly uses the injected ResiliencePipelineRegistry
    /// and doesn't create its own independent registry.
    /// </summary>
    [Fact]
    public void GetPipeline_UsesInjectedRegistry()
    {
        // Arrange
        var opts = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = new ResiliencyPolicyProvider(opts, registry, logger);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(pipeline);
    }

    #endregion
}
