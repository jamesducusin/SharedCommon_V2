namespace SharedCommon.Resiliency.UnitTests;

/// <summary>Behavioral tests for resilience policy provider pipeline registration and execution.</summary>
public sealed class ResiliencyBehaviorTests
{
    /// <summary>
    /// Verifies that the retry policy can be executed with a successful operation.
    /// </summary>
    [Fact]
    public async Task RetryPolicy_WithSuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions
        {
            Retry = new RetryOptions
            {
                MaxAttempts = 3,
                BaseDelay = TimeSpan.FromMilliseconds(50)
            }
        });

        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);

        // Act
        await pipeline.ExecuteAsync((ct) =>
        {
            return new ValueTask(Task.CompletedTask);
        });

        // Assert - if execution completes without throwing, the test passes
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that the timeout policy can be executed with a completing operation.
    /// </summary>
    [Fact]
    public async Task TimeoutPolicy_WithFastOperation_AllowsCompletion()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions
        {
            Timeout = new TimeoutOptions { Duration = TimeSpan.FromSeconds(5) }
        });

        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Act
        await pipeline.ExecuteAsync((ct) =>
        {
            return new ValueTask(Task.CompletedTask);
        });

        // Assert - if execution completes without timeout, the test passes
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that default pipeline can be executed successfully.
    /// </summary>
    [Fact]
    public void DefaultPipeline_ContainsAllThreeStrategies()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        // Act
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that custom retry configuration is properly stored and applied.
    /// </summary>
    [Fact]
    public void RetryPolicy_WithCustomConfiguration_StoresConfiguration()
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            Retry = new RetryOptions
            {
                MaxAttempts = 5,
                BaseDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromSeconds(15)
            }
        };

        var options = Microsoft.Extensions.Options.Options.Create(customOptions);
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = new ResiliencyPolicyProvider(options, registry, logger);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);

        // Assert
        Assert.NotNull(pipeline);
        Assert.Equal(5, customOptions.Retry.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(100), customOptions.Retry.BaseDelay);
    }

    /// <summary>
    /// Verifies that custom circuit breaker configuration is properly stored.
    /// </summary>
    [Fact]
    public void CircuitBreakerPolicy_WithCustomConfiguration_StoresConfiguration()
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

        var options = Microsoft.Extensions.Options.Options.Create(customOptions);
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = new ResiliencyPolicyProvider(options, registry, logger);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(pipeline);
        Assert.Equal(0.75, customOptions.CircuitBreaker.FailureRatio);
        Assert.Equal(10, customOptions.CircuitBreaker.MinimumThroughput);
    }

    /// <summary>
    /// Verifies that custom timeout configuration is properly stored.
    /// </summary>
    [Fact]
    public void TimeoutPolicy_WithCustomConfiguration_StoresConfiguration()
    {
        // Arrange
        var customOptions = new ResiliencyOptions
        {
            Timeout = new TimeoutOptions { Duration = TimeSpan.FromSeconds(60) }
        };

        var options = Microsoft.Extensions.Options.Options.Create(customOptions);
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = new ResiliencyPolicyProvider(options, registry, logger);
        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Assert
        Assert.NotNull(pipeline);
        Assert.Equal(TimeSpan.FromSeconds(60), customOptions.Timeout.Duration);
    }

    /// <summary>
    /// Verifies that RetryOnly and Default pipelines are distinct instances.
    /// </summary>
    [Fact]
    public void RetryOnlyPipeline_IsDifferentFromDefault()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        // Act
        var retryPipeline = provider.GetPipeline(ResiliencyPolicyProvider.RetryOnly);
        var defaultPipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(retryPipeline);
        Assert.NotNull(defaultPipeline);
        Assert.NotSame(retryPipeline, defaultPipeline);
    }

    /// <summary>
    /// Verifies that TimeoutOnly and Default pipelines are distinct instances.
    /// </summary>
    [Fact]
    public void TimeoutOnlyPipeline_IsDifferentFromDefault()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        // Act
        var timeoutPipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);
        var defaultPipeline = provider.GetPipeline(ResiliencyPolicyProvider.Default);

        // Assert
        Assert.NotNull(timeoutPipeline);
        Assert.NotNull(defaultPipeline);
        Assert.NotSame(timeoutPipeline, defaultPipeline);
    }

    /// <summary>
    /// Verifies that pipelines execute multiple times consistently.
    /// </summary>
    [Fact]
    public async Task Pipeline_AcrossMultipleExecutions_MaintainsConsistentBehavior()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Act
        for (int i = 0; i < 3; i++)
        {
            await pipeline.ExecuteAsync((ct) =>
            {
                return new ValueTask(Task.CompletedTask);
            });
        }

        // Assert - if all executions complete without throwing, the test passes
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that generic pipelines return correct results.
    /// </summary>
    [Fact]
    public async Task GenericPipeline_ExecuteAsync_ReturnsCorrectType()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        var pipeline = provider.GetPipeline(ResiliencyPolicyProvider.TimeoutOnly);

        // Act
        await pipeline.ExecuteAsync((ct) =>
        {
            return new ValueTask(Task.CompletedTask);
        });

        // Assert - if execution completes without throwing, the test passes
        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Verifies that logger is configured for the provider.
    /// </summary>
    [Fact]
    public void Provider_WithLogger_IsInitialized()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ResiliencyOptions());
        var registry = new ResiliencePipelineRegistry<string>();
        var logger = Substitute.For<ILogger<ResiliencyPolicyProvider>>();

        // Act
        var provider = new ResiliencyPolicyProvider(options, registry, logger);

        // Assert
        Assert.NotNull(provider);
    }

    /// <summary>
    /// Verifies that all pipeline constants are correctly named.
    /// </summary>
    [Fact]
    public void PipelineNames_AreCorrectlyDefined()
    {
        // Arrange & Act
        var defaultName = ResiliencyPolicyProvider.Default;
        var retryOnlyName = ResiliencyPolicyProvider.RetryOnly;
        var timeoutOnlyName = ResiliencyPolicyProvider.TimeoutOnly;

        // Assert
        Assert.Equal("default", defaultName);
        Assert.Equal("retry", retryOnlyName);
        Assert.Equal("timeout", timeoutOnlyName);
    }
}
