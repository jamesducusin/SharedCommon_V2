using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;

namespace SharedCommon.Resiliency;

/// <summary>
/// Builds and caches named Polly v8 resilience pipelines.
/// Pipelines are built on first access and reused thereafter.
/// </summary>
public sealed class ResiliencyPolicyProvider : IResiliencyPolicyProvider
{
    private readonly ResiliencyOptions _options;
    private readonly ResiliencePipelineRegistry<string> _registry;
    private readonly ILogger<ResiliencyPolicyProvider> _logger;

    /// <summary>Named key for the default combined pipeline (retry + circuit breaker + timeout).</summary>
    public const string Default = "default";

    /// <summary>Named key for the retry-only pipeline.</summary>
    public const string RetryOnly = "retry";

    /// <summary>Named key for the timeout-only pipeline.</summary>
    public const string TimeoutOnly = "timeout";

    /// <summary>Initializes the provider and pre-registers all standard pipelines.</summary>
    public ResiliencyPolicyProvider(
        IOptions<ResiliencyOptions> options,
        ResiliencePipelineRegistry<string> registry,
        ILogger<ResiliencyPolicyProvider> logger)
    {
        _options = options.Value;
        _registry = registry;
        _logger = logger;

        RegisterPipelines();
    }

    /// <inheritdoc />
    public ResiliencePipeline GetPipeline(string name) => _registry.GetPipeline(name);

    /// <inheritdoc />
    public ResiliencePipeline<T> GetPipeline<T>(string name) => _registry.GetPipeline<T>(name);

    private void RegisterPipelines()
    {
        _registry.TryAddBuilder(Default, (builder, _) =>
        {
            builder
                .AddRetry(BuildRetryOptions())
                .AddCircuitBreaker(BuildCircuitBreakerOptions())
                .AddTimeout(BuildTimeoutOptions());
        });

        _registry.TryAddBuilder(RetryOnly, (builder, _) =>
        {
            builder.AddRetry(BuildRetryOptions());
        });

        _registry.TryAddBuilder(TimeoutOnly, (builder, _) =>
        {
            builder.AddTimeout(BuildTimeoutOptions());
        });
    }

    private RetryStrategyOptions BuildRetryOptions() => new()
    {
        MaxRetryAttempts = _options.Retry.MaxAttempts,
        Delay = _options.Retry.BaseDelay,
        MaxDelay = _options.Retry.MaxDelay,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .Handle<BrokenCircuitException>()
    };

    private CircuitBreakerStrategyOptions BuildCircuitBreakerOptions() => new()
    {
        FailureRatio = _options.CircuitBreaker.FailureRatio,
        MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
        SamplingDuration = _options.CircuitBreaker.SamplingDuration,
        BreakDuration = _options.CircuitBreaker.BreakDuration,
        OnOpened = args =>
        {
            _logger.LogWarning("Circuit breaker opened. Break duration: {BreakDuration}",
                args.BreakDuration);
            return ValueTask.CompletedTask;
        },
        OnClosed = _ =>
        {
            _logger.LogInformation("Circuit breaker closed.");
            return ValueTask.CompletedTask;
        },
        OnHalfOpened = _ =>
        {
            _logger.LogInformation("Circuit breaker half-open. Testing next request.");
            return ValueTask.CompletedTask;
        }
    };

    private TimeoutStrategyOptions BuildTimeoutOptions() => new()
    {
        Timeout = _options.Timeout.Duration
    };
}
