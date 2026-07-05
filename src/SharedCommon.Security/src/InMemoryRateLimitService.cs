using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace SharedCommon.Security;

/// <summary>
/// Sliding-window <see cref="IRateLimitService"/> backed by in-process memory.
/// Thread-safe. Suitable for single-instance deployments or when Redis is not available.
/// </summary>
public sealed class InMemoryRateLimitService : IRateLimitService
{
    private readonly SecurityOptions _options;
    private readonly ILogger<InMemoryRateLimitService> _logger;

    // Key: "{bucket}:{identifier}" — Value: list of request timestamps in the current window.
    private readonly ConcurrentDictionary<string, RequestWindow> _windows = new();

    /// <summary>Initializes the in-memory rate limit service.</summary>
    public InMemoryRateLimitService(
        IOptions<SecurityOptions> options,
        ILogger<InMemoryRateLimitService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> AllowAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var policy = ResolvePolicy(bucket);
        var window = _windows.GetOrAdd(
            BuildKey(identifier, bucket),
            _ => new RequestWindow());

        var allowed = window.TryRecord(policy.MaxRequests, TimeSpan.FromSeconds(policy.WindowSeconds));

        if (!allowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for identifier {Identifier} on bucket {Bucket}",
                identifier, bucket);
        }

        return Task.FromResult(allowed);
    }

    /// <inheritdoc />
    public Task<RateLimitStatus> GetStatusAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var policy = ResolvePolicy(bucket);
        var window = _windows.GetOrAdd(
            BuildKey(identifier, bucket),
            _ => new RequestWindow());

        var (used, resetAt) = window.GetStatus(TimeSpan.FromSeconds(policy.WindowSeconds));
        var remaining = Math.Max(0, policy.MaxRequests - used);

        return Task.FromResult(new RateLimitStatus(
            RequestsAllowed: policy.MaxRequests,
            RequestsUsed: used,
            RequestsRemaining: remaining,
            ResetAt: resetAt));
    }

    /// <inheritdoc />
    public Task ResetAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        _windows.TryRemove(BuildKey(identifier, bucket), out _);
        return Task.CompletedTask;
    }

    private RateLimitPolicyOptions ResolvePolicy(string bucket)
    {
        if (_options.RateLimit.Policies.TryGetValue(bucket, out var policy))
            return policy;

        // Fall back to "Default" policy; if that's missing too, use safe hard-coded limits.
        return _options.RateLimit.Policies.TryGetValue("Default", out var def)
            ? def
            : new RateLimitPolicyOptions { MaxRequests = 100, WindowSeconds = 60 };
    }

    private static string BuildKey(string identifier, string bucket)
        => $"{bucket}:{identifier}";

    private sealed class RequestWindow
    {
        private readonly object _lock = new();
        private readonly List<DateTimeOffset> _timestamps = [];

        public bool TryRecord(int maxRequests, TimeSpan window)
        {
            lock (_lock)
            {
                Evict(window);
                if (_timestamps.Count >= maxRequests) return false;
                _timestamps.Add(DateTimeOffset.UtcNow);
                return true;
            }
        }

        public (int Used, DateTimeOffset ResetAt) GetStatus(TimeSpan window)
        {
            lock (_lock)
            {
                Evict(window);
                var resetAt = _timestamps.Count > 0
                    ? _timestamps[0].Add(window)
                    : DateTimeOffset.UtcNow.Add(window);
                return (_timestamps.Count, resetAt);
            }
        }

        private void Evict(TimeSpan window)
        {
            var cutoff = DateTimeOffset.UtcNow - window;
            _timestamps.RemoveAll(t => t < cutoff);
        }
    }
}
