using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Resiliency.RateLimiting;

/// <summary>
/// ASP.NET Core middleware for distributed rate limiting.
/// Applies rate limits based on user ID, API key, or IP address with per-instance consistency.
/// </summary>
/// <remarks>
/// Rate limit key priority:
/// 1. Authenticated user ID (from "sub" claim)
/// 2. X-API-Key header
/// 3. Remote IP address
///
/// Adds rate limit headers to all responses:
/// - X-RateLimit-Limit: maximum requests in window
/// - X-RateLimit-Remaining: tokens remaining in current window
/// - X-RateLimit-Reset: Unix timestamp when window resets
/// - Retry-After: seconds to wait (only when rate limited)
/// </remarks>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IDistributedRateLimiter _rateLimiter;
    private readonly RateLimiterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <param name="rateLimiter">Distributed rate limiter service</param>
    /// <param name="options">Rate limiter configuration options</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IDistributedRateLimiter rateLimiter,
        RateLimiterOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the middleware to check and enforce rate limits.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || IsExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var rateLimitKey = GetRateLimitKey(context);
            var result = await _rateLimiter.TryAcquireAsync(rateLimitKey, tokens: 1);

            if (!result.Allowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for {KeyType}: {KeyHash}",
                    GetKeyType(context),
                    HashKey(rateLimitKey));

                // Add rate limit headers
                AddRateLimitHeaders(context.Response, result, _options);

                context.Response.StatusCode = _options.StatusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = $"Rate limit exceeded. Retry after {result.RetryAfter?.TotalSeconds:F0} seconds.",
                    retryAfter = result.RetryAfter?.TotalSeconds
                });

                return;
            }

            // Add rate limit headers for allowed requests
            context.Response.OnStarting(() =>
            {
                AddRateLimitHeaders(context.Response, result, _options);
                return Task.CompletedTask;
            });

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware");
            // Fail open on middleware errors
            await _next(context);
        }
    }

    /// <summary>
    /// Gets the rate limit key based on authentication and request context.
    /// Uses priority ordering: authenticated user &gt; API key &gt; IP address.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The rate limit key for the request</returns>
    private static string GetRateLimitKey(HttpContext context)
    {
        // Priority 1: Authenticated user (sub claim)
        if (context.User?.FindFirst("sub")?.Value is string userId)
            return $"user:{userId}";

        // Priority 2: API key from header
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey.ToString()))
            return $"apikey:{apiKey}";

        // Priority 3: IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    /// <summary>
    /// Gets the key type for logging purposes (user, apikey, or ip).
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The type of rate limit key being used</returns>
    private static string GetKeyType(HttpContext context)
    {
        if (context.User?.FindFirst("sub")?.Value is not null) return "user";
        if (context.Request.Headers.ContainsKey("X-API-Key")) return "apikey";
        return "ip";
    }

    /// <summary>
    /// Hashes a rate limit key for logging (prevents sensitive data exposure).
    /// </summary>
    /// <param name="key">The key to hash</param>
    /// <returns>First 8 bytes of SHA256 hash as hex string</returns>
    private static string HashKey(string key)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(key));
        return BitConverter.ToString(hash, 0, 8).Replace("-", "");
    }

    /// <summary>
    /// Determines if a request path is exempt from rate limiting.
    /// </summary>
    /// <param name="path">The request path</param>
    /// <returns>True if the path is exempt from rate limiting</returns>
    private static bool IsExemptPath(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/metrics") ||
               path.StartsWithSegments("/swagger");
    }

    /// <summary>
    /// Adds standard rate limit headers to the response.
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="result">The rate limit result</param>
    /// <param name="options">Rate limiter options for limit value</param>
    private static void AddRateLimitHeaders(HttpResponse response, RateLimitResult result, RateLimiterOptions options)
    {
        response.Headers["X-RateLimit-Limit"] = options.Limit.ToString();
        response.Headers["X-RateLimit-Remaining"] = result.TokensRemaining.ToString();
        response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(result.ResetTime).ToUnixTimeSeconds().ToString();
        
        if (result.RetryAfter.HasValue)
            response.Headers["Retry-After"] = result.RetryAfter.Value.TotalSeconds.ToString("F0");
    }
}

/// <summary>
/// Extension methods for registering distributed rate limiting in the service collection and middleware pipeline.
/// </summary>
public static class RateLimitingServiceExtensions
{
    /// <summary>
    /// Registers distributed rate limiting in the dependency injection container.
    /// Requires <see cref="StackExchange.Redis.IConnectionMultiplexer"/> to be registered separately.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration delegate for <see cref="RateLimiterOptions"/></param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    /// <example>
    /// <code>
    /// services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConnectionString);
    /// services.AddDistributedRateLimiting(opts => {
    ///     opts.Limit = 100;
    ///     opts.WindowSeconds = 60;
    ///     opts.Enabled = true;
    /// });
    ///
    /// app.UseDistributedRateLimiting();
    /// </code>
    /// </example>
    public static IServiceCollection AddDistributedRateLimiting(
        this IServiceCollection services,
        Action<RateLimiterOptions>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var options = new RateLimiterOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IDistributedRateLimiter, DistributedRateLimiter>();

        return services;
    }

    /// <summary>
    /// Adds distributed rate limiting middleware to the ASP.NET Core pipeline.
    /// Must be registered after Redis and before routing middleware.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when app is null</exception>
    public static IApplicationBuilder UseDistributedRateLimiting(this IApplicationBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}
