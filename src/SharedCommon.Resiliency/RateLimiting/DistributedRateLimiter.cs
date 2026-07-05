using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SharedCommon.Resiliency.RateLimiting;

/// <summary>
/// Distributed rate limiter using Redis for multi-instance deployments.
/// Implements token bucket algorithm with configurable limits per key (user, IP, API endpoint).
/// </summary>
public class DistributedRateLimiter : IDistributedRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimiterOptions _options;

    public DistributedRateLimiter(IConnectionMultiplexer redis, RateLimiterOptions options)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Attempts to acquire a token within the rate limit.
    /// Returns true if allowed, false if rate limited.
    /// </summary>
    public async Task<RateLimitResult> TryAcquireAsync(string key, int tokens = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (tokens <= 0) throw new ArgumentOutOfRangeException(nameof(tokens), "Tokens must be > 0");

        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";
        var windowKey = $"{redisKey}:window";

        try
        {
            var result = await ExecuteRateLimitLuaAsync(db, redisKey, windowKey, tokens, cancellationToken);
            
            return new RateLimitResult
            {
                Allowed = result.Allowed,
                TokensRemaining = result.TokensRemaining,
                ResetTime = result.ResetTime,
                RetryAfter = result.RetryAfter
            };
        }
        catch (Exception ex)
        {
            // Fail open on Redis errors - don't block traffic due to cache failure
            return new RateLimitResult
            {
                Allowed = true,
                TokensRemaining = _options.Limit,
                ResetTime = DateTime.UtcNow.AddSeconds(_options.WindowSeconds),
                ErrorOccurred = true
            };
        }
    }

    private async Task<(bool Allowed, int TokensRemaining, DateTime ResetTime, TimeSpan? RetryAfter)> ExecuteRateLimitLuaAsync(
        IDatabase db, string counterKey, string windowKey, int tokensRequested, CancellationToken cancellationToken)
    {
        var lua = @"
local counter_key = KEYS[1]
local window_key = KEYS[2]
local tokens_requested = tonumber(ARGV[1])
local limit = tonumber(ARGV[2])
local window_seconds = tonumber(ARGV[3])
local now = tonumber(ARGV[4])

local window_start = redis.call('get', window_key)
if not window_start then
    window_start = now
    redis.call('set', window_key, window_start, 'EX', window_seconds)
else
    window_start = tonumber(window_start)
end

local elapsed = now - window_start
if elapsed >= window_seconds then
    -- Window expired, reset
    redis.call('set', window_key, now, 'EX', window_seconds)
    redis.call('set', counter_key, limit - tokens_requested, 'EX', window_seconds)
    return {1, limit - tokens_requested, now + window_seconds, 0}
end

local current = redis.call('get', counter_key)
if not current then
    current = limit
else
    current = tonumber(current)
end

if current >= tokens_requested then
    local remaining = current - tokens_requested
    redis.call('decrby', counter_key, tokens_requested)
    redis.call('expire', counter_key, window_seconds - elapsed)
    return {1, remaining, window_start + window_seconds, 0}
else
    -- Rate limited
    local retry_after = window_start + window_seconds - now
    return {0, 0, window_start + window_seconds, retry_after}
end
";

        var keys = new RedisKey[] { counterKey, windowKey };
        var values = new RedisValue[]
        {
            tokensRequested,
            _options.Limit,
            _options.WindowSeconds,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var result = await db.ScriptEvaluateAsync(lua, keys, values);
        
        if (result.IsNull)
        {
            throw new InvalidOperationException("Lua script returned null");
        }

        var resultArray = (RedisResult[])result;
        var allowed = (long)resultArray[0] == 1;
        var tokensRemaining = (int)(long)resultArray[1];
        var resetTime = DateTimeOffset.FromUnixTimeSeconds((long)resultArray[2]).DateTime;
        var retryAfter = (long)resultArray[3] > 0 ? TimeSpan.FromSeconds((long)resultArray[3]) : (TimeSpan?)null;

        return (allowed, tokensRemaining, resetTime, retryAfter);
    }

    /// <summary>
    /// Resets the rate limit counter for a key (admin operation).
    /// </summary>
    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";
        var windowKey = $"{redisKey}:window";

        await db.KeyDeleteAsync(new[] { (RedisKey)redisKey, (RedisKey)windowKey });
    }

    /// <summary>
    /// Gets current rate limit status for a key (monitoring).
    /// </summary>
    public async Task<RateLimitStatus> GetStatusAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";
        var windowKey = $"{redisKey}:window";

        try
        {
            var current = await db.StringGetAsync(redisKey);
            var windowStart = await db.StringGetAsync(windowKey);

            if (!windowStart.HasValue)
            {
                return new RateLimitStatus
                {
                    Key = key,
                    TokensRemaining = _options.Limit,
                    ResetTime = DateTime.UtcNow.AddSeconds(_options.WindowSeconds),
                    WindowPercentUsed = 0
                };
            }

            var windowStartTime = long.Parse(windowStart.ToString());
            var windowResetTime = DateTimeOffset.FromUnixTimeSeconds(windowStartTime + _options.WindowSeconds).DateTime;
            var tokensRemaining = current.HasValue ? int.Parse(current.ToString()) : _options.Limit;
            var tokensUsed = _options.Limit - tokensRemaining;
            var windowPercentUsed = (double)tokensUsed / _options.Limit * 100;

            return new RateLimitStatus
            {
                Key = key,
                TokensRemaining = tokensRemaining,
                ResetTime = windowResetTime,
                WindowPercentUsed = windowPercentUsed
            };
        }
        catch (Exception)
        {
            return new RateLimitStatus
            {
                Key = key,
                TokensRemaining = _options.Limit,
                ResetTime = DateTime.UtcNow.AddSeconds(_options.WindowSeconds),
                ErrorOccurred = true
            };
        }
    }
}

public interface IDistributedRateLimiter
{
    Task<RateLimitResult> TryAcquireAsync(string key, int tokens = 1, CancellationToken cancellationToken = default);
    Task ResetAsync(string key, CancellationToken cancellationToken = default);
    Task<RateLimitStatus> GetStatusAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for distributed rate limiter.
/// </summary>
public class RateLimiterOptions
{
    /// <summary>
    /// Maximum tokens per window. Default: 100 requests.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds. Default: 60 seconds (1 minute).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// HTTP status code to return when rate limited. Default: 429 Too Many Requests.
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// Enable rate limiting. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed within rate limit.
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Tokens remaining in current window.
    /// </summary>
    public int TokensRemaining { get; set; }

    /// <summary>
    /// Time when the rate limit window resets.
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// How long to wait before retrying (if rate limited).
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Whether an error occurred in rate limit check (fail-open mode).
    /// </summary>
    public bool ErrorOccurred { get; set; }
}

public class RateLimitStatus
{
    public string Key { get; set; }
    public int TokensRemaining { get; set; }
    public DateTime ResetTime { get; set; }
    public double WindowPercentUsed { get; set; }
    public bool ErrorOccurred { get; set; }
}
