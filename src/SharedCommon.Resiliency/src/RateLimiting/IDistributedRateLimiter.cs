using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SharedCommon.Resiliency.RateLimiting;

/// <summary>
/// Distributed rate limiter using Redis for multi-instance deployments.
/// Implements token bucket algorithm with configurable limits per key (user, IP, API endpoint).
/// </summary>
/// <remarks>
/// Uses Lua scripts for atomic operations to prevent race conditions.
/// Supports three key types: user ID (priority 1), API key (priority 2), IP address (priority 3).
/// Fails open on Redis errors - never blocks traffic due to cache failures.
/// </remarks>
public interface IDistributedRateLimiter
{
    /// <summary>
    /// Attempts to acquire a token within the rate limit.
    /// </summary>
    /// <param name="key">The rate limit key (user ID, API key, or IP address)</param>
    /// <param name="tokens">Number of tokens to request. Default: 1.</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Rate limit result indicating allowed/denied and retry information</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null or empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tokens is &lt;= 0</exception>
    Task<RateLimitResult> TryAcquireAsync(string key, int tokens = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit counter for a key (admin operation).
    /// </summary>
    /// <param name="key">The rate limit key to reset</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null or empty</exception>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current rate limit status for a key (monitoring/diagnostics).
    /// </summary>
    /// <param name="key">The rate limit key to check</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Current rate limit status including tokens remaining and reset time</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null or empty</exception>
    Task<RateLimitStatus> GetStatusAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a rate limit check operation.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Gets or sets whether the request is allowed within the rate limit.
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Gets or sets the tokens remaining in the current window.
    /// </summary>
    public int TokensRemaining { get; set; }

    /// <summary>
    /// Gets or sets the time when the rate limit window resets.
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Gets or sets how long to wait before retrying (only set if rate limited).
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Gets or sets whether an error occurred in the rate limit check (fail-open mode).
    /// </summary>
    public bool ErrorOccurred { get; set; }
}

/// <summary>
/// Current rate limit status for monitoring and diagnostics.
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Gets or sets the rate limit key being monitored.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tokens remaining in the current window.
    /// </summary>
    public int TokensRemaining { get; set; }

    /// <summary>
    /// Gets or sets when the current window resets.
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the window used (0-100).
    /// </summary>
    public double WindowPercentUsed { get; set; }

    /// <summary>
    /// Gets or sets whether an error occurred retrieving status.
    /// </summary>
    public bool ErrorOccurred { get; set; }
}

/// <summary>
/// Configuration options for distributed rate limiting.
/// </summary>
public class RateLimiterOptions
{
    /// <summary>
    /// Gets or sets the maximum tokens allowed per window.
    /// Default: 100 requests per window.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the duration of the rate limit window in seconds.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the HTTP status code to return when rate limited.
    /// Default: 429 (Too Many Requests).
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable strict mode (fail closed).
    /// When false (default), Redis errors allow requests through (fail open).
    /// When true, Redis errors block all requests.
    /// </summary>
    public bool StrictMode { get; set; } = false;
}
