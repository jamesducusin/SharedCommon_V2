using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Resiliency;

/// <summary>Top-level configuration for all SharedCommon resilience pipelines.</summary>
public sealed class ResiliencyOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Resiliency";

    /// <summary>Retry policy configuration.</summary>
    public RetryOptions Retry { get; init; } = new();

    /// <summary>Circuit breaker policy configuration.</summary>
    public CircuitBreakerOptions CircuitBreaker { get; init; } = new();

    /// <summary>Timeout policy configuration.</summary>
    public TimeoutOptions Timeout { get; init; } = new();
}

/// <summary>Exponential back-off retry configuration.</summary>
public sealed class RetryOptions
{
    /// <summary>Maximum number of retry attempts. Defaults to 3.</summary>
    [Range(1, 10)]
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Base delay for the first retry. Subsequent retries grow exponentially with jitter.</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Maximum delay cap for any single retry.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>Circuit breaker configuration.</summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Failure ratio (0–1) needed to open the circuit. Defaults to 0.5 (50%).</summary>
    [Range(0.0, 1.0)]
    public double FailureRatio { get; init; } = 0.5;

    /// <summary>Minimum number of requests before failure ratio is evaluated. Defaults to 5.</summary>
    [Range(1, 100)]
    public int MinimumThroughput { get; init; } = 5;

    /// <summary>Window over which the failure ratio is measured. Defaults to 60 seconds.</summary>
    public TimeSpan SamplingDuration { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>How long the circuit stays Open before transitioning to Half-Open. Defaults to 30 seconds.</summary>
    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>Timeout policy configuration.</summary>
public sealed class TimeoutOptions
{
    /// <summary>Maximum time an operation is allowed to run. Defaults to 30 seconds.</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(30);
}
