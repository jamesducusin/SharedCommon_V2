using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedCommon.Observability;

namespace SharedCommon.Resiliency.RateLimiting;

/// <summary>
/// Middleware for distributed rate limiting.
/// Applies rate limits based on user ID, API key, or IP address.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IDistributedRateLimiter _rateLimiter;
    private readonly RateLimiterOptions _options;
    private readonly ITelemetryService _telemetry;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IDistributedRateLimiter rateLimiter,
        RateLimiterOptions options,
        ITelemetryService telemetry)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || IsExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        using var scope = _telemetry.StartOperation("RateLimitCheck", "middleware");

        try
        {
            // Determine the rate limit key (user ID > API key > IP address)
            var rateLimitKey = GetRateLimitKey(context);
            scope.SetTag("ratelimit.key_type", GetKeyType(context));
            scope.SetTag("ratelimit.key_hash", HashKey(rateLimitKey));

            // Check rate limit
            var result = await _rateLimiter.TryAcquireAsync(rateLimitKey, tokens: 1);

            _telemetry.RecordMetric("ratelimit.check", 1, new()
            {
                { "allowed", result.Allowed.ToString().ToLower() },
                { "endpoint", context.Request.Path.ToString() },
                { "tokens_remaining", result.TokensRemaining.ToString() }
            });

            if (!result.Allowed)
            {
                _logger.LogWarning("Rate limit exceeded for key: {RateLimitKey}", HashKey(rateLimitKey));
                
                // Add rate limit headers
                context.Response.Headers["Retry-After"] = result.RetryAfter?.TotalSeconds.ToString("0");
                context.Response.Headers["X-RateLimit-Limit"] = _options.Limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = result.TokensRemaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(result.ResetTime).ToUnixTimeSeconds().ToString();

                scope.RecordException(new InvalidOperationException("Rate limit exceeded"));
                context.Response.StatusCode = _options.StatusCode;
                
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "Rate limit exceeded. Please retry after " + result.RetryAfter?.TotalSeconds.ToString("0") + " seconds.",
                    retryAfter = result.RetryAfter?.TotalSeconds
                });

                return;
            }

            // Add rate limit info headers for allowed requests
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = _options.Limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = result.TokensRemaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(result.ResetTime).ToUnixTimeSeconds().ToString();
                return Task.CompletedTask;
            });

            scope.MarkSucceeded();
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware");
            scope.RecordException(ex);
            // Fail open - allow request if rate limiter fails
            await _next(context);
        }
    }

    /// <summary>
    /// Gets the rate limit key based on authentication context.
    /// Priority: Authenticated user ID > API key > IP address
    /// </summary>
    private string GetRateLimitKey(HttpContext context)
    {
        // Priority 1: Authenticated user
        if (context.User?.FindFirst("sub")?.Value is string userId)
        {
            return $"user:{userId}";
        }

        // Priority 2: API key from header
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey.ToString()))
        {
            return $"apikey:{apiKey}";
        }

        // Priority 3: IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private string GetKeyType(HttpContext context)
    {
        if (context.User?.FindFirst("sub")?.Value is not null) return "user";
        if (context.Request.Headers.ContainsKey("X-API-Key")) return "apikey";
        return "ip";
    }

    /// <summary>
    /// Hashes the rate limit key for logging (privacy).
    /// </summary>
    private string HashKey(string key)
    {
        return System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(key)
        ).AsSpan(0, 8).ToArray().ToString() ?? key;
    }

    /// <summary>
    /// Exempts certain paths from rate limiting (health checks, etc).
    /// </summary>
    private bool IsExemptPath(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/metrics") ||
               path.StartsWithSegments("/swagger");
    }
}

public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Registers distributed rate limiting in the DI container and middleware pipeline.
    /// </summary>
    public static IServiceCollection AddDistributedRateLimiting(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        var options = new RateLimiterOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IDistributedRateLimiter, DistributedRateLimiter>();

        return services;
    }

    /// <summary>
    /// Adds rate limiting middleware to the pipeline.
    /// Must be called after Redis is registered and before routing.
    /// </summary>
    public static IApplicationBuilder UseDistributedRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}
