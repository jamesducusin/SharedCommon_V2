namespace SharedCommon.Security;

/// <summary>
/// Sliding-window rate limiter. Tracks request counts per identifier and bucket (policy name).
///
/// Example:
/// <code>
/// var allowed = await _rateLimiter.AllowAsync(
///     identifier: Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
///     bucket: "Default",
///     ct: ct);
///
/// if (!allowed)
///     return StatusCode(429);
/// </code>
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Determines whether a new request from <paramref name="identifier"/> is within the
    /// limits of <paramref name="bucket"/>. Increments the counter on <c>true</c>.
    /// </summary>
    /// <param name="identifier">Client identifier — typically an IP address, user ID, or API key.</param>
    /// <param name="bucket">Policy name, matching a key in <see cref="RateLimitOptions.Policies"/>. Default: <c>"default"</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the request is within limits; <c>false</c> if it should be rejected.</returns>
    Task<bool> AllowAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default);

    /// <summary>
    /// Returns the current request count and limit state for the given identifier and bucket.
    /// </summary>
    /// <param name="identifier">Client identifier.</param>
    /// <param name="bucket">Policy name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<RateLimitStatus> GetStatusAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default);

    /// <summary>
    /// Clears the counter for the given identifier and bucket.
    /// Primarily used for testing or administrative overrides.
    /// </summary>
    /// <param name="identifier">Client identifier.</param>
    /// <param name="bucket">Policy name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ResetAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default);
}

/// <summary>
/// Snapshot of the rate limit state for a specific identifier + bucket pair.
/// </summary>
/// <param name="RequestsAllowed">Maximum requests allowed in the window.</param>
/// <param name="RequestsUsed">Requests consumed so far in the current window.</param>
/// <param name="RequestsRemaining">Requests still available in the current window.</param>
/// <param name="ResetAt">UTC time when the current window expires and counters reset.</param>
public record RateLimitStatus(
    int RequestsAllowed,
    int RequestsUsed,
    int RequestsRemaining,
    DateTimeOffset ResetAt);
