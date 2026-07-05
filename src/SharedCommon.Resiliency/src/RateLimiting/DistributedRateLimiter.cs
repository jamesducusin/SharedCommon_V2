using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SharedCommon.Resiliency.RateLimiting;

/// <summary>
/// Redis-backed implementation of <see cref="IDistributedRateLimiter"/>.
/// Uses Lua scripts for atomic token bucket operations across multiple instances.
/// </summary>
public sealed class DistributedRateLimiter : IDistributedRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimiterOptions _options;
    private readonly ILogger<DistributedRateLimiter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedRateLimiter"/> class.
    /// </summary>
    /// <param name="redis">Redis connection multiplexer for distributed state</param>
    /// <param name="options">Rate limiter configuration options</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when redis, options, or logger is null</exception>
    public DistributedRateLimiter(
        IConnectionMultiplexer redis,
        RateLimiterOptions options,
        ILogger<DistributedRateLimiter> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> TryAcquireAsync(string key, int tokens = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        if (tokens <= 0) throw new ArgumentOutOfRangeException(nameof(tokens), "Tokens must be positive");

        try
        {
            var db = _redis.GetDatabase();
            var redisKey = $"ratelimit:{key}";
            var windowKey = $"{redisKey}:window";

            var result = await ExecuteRateLimitLuaAsync(db, redisKey, windowKey, tokens, cancellationToken);

            _logger.LogDebug(
                "Rate limit check: Key={Key}, Allowed={Allowed}, TokensRemaining={TokensRemaining}",
                key, result.Allowed, result.TokensRemaining);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key: {Key}", key);
            
            // Fail open by default (allow request), unless strict mode enabled
            if (_options.StrictMode)
                throw;

            return new RateLimitResult
            {
                Allowed = true,
                TokensRemaining = _options.Limit,
                ResetTime = DateTime.UtcNow.AddSeconds(_options.WindowSeconds),
                ErrorOccurred = true
            };
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        try
        {
            var db = _redis.GetDatabase();
            var redisKey = $"ratelimit:{key}";
            var windowKey = $"{redisKey}:window";

            await db.KeyDeleteAsync(new[] { (RedisKey)redisKey, (RedisKey)windowKey });
            _logger.LogInformation("Rate limit reset for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for key: {Key}", key);
            if (_options.StrictMode)
                throw;
        }
    }

    /// <inheritdoc />
    public async Task<RateLimitStatus> GetStatusAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        try
        {
            var db = _redis.GetDatabase();
            var redisKey = $"ratelimit:{key}";
            var windowKey = $"{redisKey}:window";

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit status for key: {Key}", key);
            
            return new RateLimitStatus
            {
                Key = key,
                TokensRemaining = _options.Limit,
                ResetTime = DateTime.UtcNow.AddSeconds(_options.WindowSeconds),
                ErrorOccurred = true
            };
        }
    }

    private async Task<RateLimitResult> ExecuteRateLimitLuaAsync(
        IDatabase db,
        string counterKey,
        string windowKey,
        int tokensRequested,
        CancellationToken cancellationToken)
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
            throw new InvalidOperationException("Lua script evaluation returned null");

        var resultArray = (RedisResult[]?)result;
        if (resultArray == null)
            throw new InvalidOperationException("Lua script returned invalid result format");

        var allowed = (long)resultArray[0] == 1;
        var tokensRemaining = (int)(long)resultArray[1];
        var resetTime = DateTimeOffset.FromUnixTimeSeconds((long)resultArray[2]).DateTime;
        var retryAfter = (long)resultArray[3] > 0 ? TimeSpan.FromSeconds((long)resultArray[3]) : (TimeSpan?)null;

        return new RateLimitResult
        {
            Allowed = allowed,
            TokensRemaining = tokensRemaining,
            ResetTime = resetTime,
            RetryAfter = retryAfter
        };
    }
}
