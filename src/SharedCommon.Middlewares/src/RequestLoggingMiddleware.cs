using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedCommon.Core;
using System.Diagnostics;
using System.Text;

namespace SharedCommon.Middlewares;

/// <summary>
/// Logs the HTTP method, path, status code, and duration of every request.
/// Optionally captures request and response bodies (disabled by default).
/// Health and metrics paths are excluded to suppress noise.
///
/// Register via <see cref="ApplicationBuilderExtensions.UseSharedCommonRequestLogging"/>.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MiddlewareOptions _options;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>Initializes the middleware.</summary>
    public RequestLoggingMiddleware(
        RequestDelegate next,
        IOptions<MiddlewareOptions> options,
        ILogger<RequestLoggingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var requestLogging = _options.RequestLogging;

        if (!requestLogging.Enabled || IsExcluded(context.Request.Path, requestLogging.ExcludePaths))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var correlationId = GetCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        string? requestBody = null;
        if (requestLogging.LogRequestBody)
            requestBody = await ReadBodyAsync(context.Request, requestLogging.MaxBodySizeToLog).ConfigureAwait(false);

        _logger.LogInformation(
            "HTTP {Method} {Path}{QueryString} started — CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            correlationId);

        if (requestBody is not null)
            _logger.LogDebug("Request body: {RequestBody}", requestBody);

        await _next(context).ConfigureAwait(false);

        stopwatch.Stop();

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms — CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            correlationId);
    }

    private static bool IsExcluded(PathString path, string[] excludePaths)
    {
        if (excludePaths.Length == 0) return false;
        return excludePaths.Any(p =>
            path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.RequestServices.GetService(typeof(IRequestContext)) is IRequestContext rc
            && !string.IsNullOrWhiteSpace(rc.CorrelationId.Value))
        {
            return rc.CorrelationId.Value;
        }

        return context.Request.Headers.TryGetValue("X-Correlation-ID", out var h) ? h.ToString() : "-";
    }

    private static async Task<string?> ReadBodyAsync(HttpRequest request, int maxBytes)
    {
        if (!request.Body.CanRead || !request.Body.CanSeek) return null;

        request.EnableBuffering();
        request.Body.Position = 0;

        var buffer = new byte[Math.Min(maxBytes, 65536)];
        var read = await request.Body.ReadAsync(buffer).ConfigureAwait(false);
        request.Body.Position = 0;

        if (read == 0) return null;

        return Encoding.UTF8.GetString(buffer, 0, read)
               + (read == maxBytes ? " [truncated]" : string.Empty);
    }
}
