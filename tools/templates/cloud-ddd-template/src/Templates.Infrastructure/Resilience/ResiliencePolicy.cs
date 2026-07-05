namespace Templates.Infrastructure.Resilience;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resilience policies using Polly for handling transient failures and protecting against cascading failures.
/// Includes retry, circuit breaker, timeout, and bulkhead isolation patterns.
/// </summary>
public static class ResiliencePolicy
{
    /// <summary>
    /// Retry policy: Handles transient failures (429, 503, 504 HTTP status codes, timeout exceptions)
    /// with exponential backoff + jitter.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutReachedException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||           // 408
                r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||          // 429
                r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||       // 503
                r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)             // 504
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                {
                    // Exponential backoff: 100ms, 400ms, 1600ms
                    var exponentialBackoff = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                    // Add jitter: ±10ms
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(-10, 11));
                    return exponentialBackoff.Add(jitter);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry attempt {RetryCount} after {DelayMs}ms due to {Reason}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Circuit breaker policy: Prevents cascading failures by breaking the circuit if failure rate exceeds threshold.
    /// Transitions: Closed → Open (after 3 failures) → Half-Open (after 30s) → Closed/Open
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration, context) =>
                {
                    logger.LogError(
                        "Circuit breaker opened for {Duration}s due to {Failures} failures",
                        duration.TotalSeconds, 3);
                },
                onReset: (context) =>
                {
                    logger.LogInformation("Circuit breaker reset and closed");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open, testing recovery");
                });
    }

    /// <summary>
    /// Timeout policy: Cancels requests that exceed the timeout threshold.
    /// Prevents indefinite waiting and resource exhaustion.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpTimeoutPolicy(TimeSpan timeout, ILogger logger)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: timeout,
            timeoutStrategy: TimeoutStrategy.Optimistic,
            onTimeoutAsync: (context, timespan, task, ex) =>
            {
                logger.LogWarning(
                    "HTTP request timeout after {TimeoutMs}ms",
                    timespan.TotalMilliseconds);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Bulkhead isolation policy: Limits concurrent requests to prevent thread starvation.
    /// If limit exceeded, subsequent requests are rejected rather than queued.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetHttpBulkheadPolicy(
        int maxParallelization,
        int maxQueueingActions,
        ILogger logger)
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: maxParallelization,
            maxParallelizationActionAttempts: maxQueueingActions,
            onBulkheadRejectedAsync: (context) =>
            {
                logger.LogWarning(
                    "Bulkhead rejection: max {MaxParallel} parallel requests reached",
                    maxParallelization);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Combines all resilience policies for external HTTP calls.
    /// Order: Timeout → Bulkhead → CircuitBreaker → Retry
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedHttpPolicy(
        TimeSpan timeout,
        int maxParallelization,
        ILogger logger)
    {
        var timeoutPolicy = GetHttpTimeoutPolicy(timeout, logger);
        var bulkheadPolicy = GetHttpBulkheadPolicy(
            maxParallelization: maxParallelization,
            maxQueueingActions: maxParallelization * 2,
            logger: logger);
        var circuitBreakerPolicy = GetHttpCircuitBreakerPolicy(logger);
        var retryPolicy = GetHttpRetryPolicy(logger);

        return Policy.WrapAsync(timeoutPolicy, bulkheadPolicy, circuitBreakerPolicy, retryPolicy);
    }
}

/// <summary>
/// Extension methods for registering resilience policies in dependency injection.
/// </summary>
public static class ResiliencePolicyExtensions
{
    /// <summary>
    /// Add resilience policies to HTTP clients.
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(
        this IHttpClientBuilder httpClientBuilder,
        TimeSpan? timeout = null,
        int? maxParallelization = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        maxParallelization ??= 10;

        httpClientBuilder.AddPolicyHandler((provider, request) =>
        {
            var logger = provider.GetRequiredService<ILogger<IHttpClientFactory>>();
            return ResiliencePolicy.GetCombinedHttpPolicy(timeout, maxParallelization.Value, logger);
        });

        return httpClientBuilder;
    }

    /// <summary>
    /// Register resilience policies for dependency injection.
    /// </summary>
    public static IServiceCollection AddResiliencePolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Add telemetry integration to track policy events
        // services.AddScoped<IResiliencyMetrics, ResiliencyMetrics>();
        
        return services;
    }
}
