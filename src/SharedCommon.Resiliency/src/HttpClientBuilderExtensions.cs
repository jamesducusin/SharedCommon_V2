using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace SharedCommon.Resiliency;

/// <summary>Extension methods for applying SharedCommon resilience pipelines to named HTTP clients.</summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Applies the SharedCommon standard resilience pipeline (retry + circuit breaker + timeout)
    /// to the HTTP client, sourcing configuration from <see cref="ResiliencyOptions"/>.
    /// </summary>
    public static IHttpClientBuilder AddSharedResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("shared", (pipeline, ctx) =>
        {
            var options = ctx.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<ResiliencyOptions>>()
                .Value;

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.Retry.MaxAttempts,
                Delay = options.Retry.BaseDelay,
                MaxDelay = options.Retry.MaxDelay,
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration = options.CircuitBreaker.SamplingDuration,
                BreakDuration = options.CircuitBreaker.BreakDuration
            });

            pipeline.AddTimeout(options.Timeout.Duration);
        });

        return builder;
    }
}
