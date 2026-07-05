using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedCommon.Core;
using SharedCommon.Core.Exceptions;
using System.Text.Json;

namespace SharedCommon.Middlewares;

/// <summary>
/// Catches all unhandled exceptions and writes a standardized JSON error response.
///
/// Response format:
/// <code>
/// {
///   "success": false,
///   "error": {
///     "code": "NOT_FOUND",
///     "message": "Order 123 was not found.",
///     "correlationId": "...",
///     "timestamp": "2026-05-09T14:30:00Z",
///     "stackTrace": null
///   }
/// }
/// </code>
///
/// Register first in the pipeline via
/// <see cref="ApplicationBuilderExtensions.UseSharedCommonExceptionHandling"/>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MiddlewareOptions _options;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Initializes the middleware.</summary>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        IOptions<MiddlewareOptions> options,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = GetCorrelationId(context);

        var (statusCode, code, message) = exception switch
        {
            NotFoundException e => (404, e.Code, e.Message),
            UnauthorizedException e => (401, e.Code, e.Message),
            ForbiddenException e => (403, e.Code, e.Message),
            ConflictException e => (409, e.Code, e.Message),
            TooManyRequestsException e => (429, e.Code, e.Message),
            DomainException e => (e.StatusCode, e.Code, e.Message),
            OperationCanceledException => (499, "CANCELLED", "The request was cancelled."),
            _ => (500, "UNHANDLED_EXCEPTION", "An unexpected error occurred.")
        };

        if (_options.ExceptionHandling.LogExceptions)
        {
            if (statusCode >= 500)
                _logger.LogError(exception, "Unhandled exception [{Code}] for {Method} {Path} — {CorrelationId}",
                    code, context.Request.Method, context.Request.Path, correlationId);
            else
                _logger.LogWarning("Domain exception [{Code}] for {Method} {Path} — {Message}",
                    code, context.Request.Method, context.Request.Path, message);
        }

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var body = new
            {
                success = false,
                error = new
                {
                    code,
                    message,
                    correlationId,
                    timestamp = DateTimeOffset.UtcNow,
                    stackTrace = _options.ExceptionHandling.IncludeStackTrace
                        ? exception.ToString()
                        : (string?)null
                }
            };

            await context.Response
                .WriteAsync(JsonSerializer.Serialize(body, _jsonOpts))
                .ConfigureAwait(false);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        if (context.RequestServices.GetService(typeof(IRequestContext)) is IRequestContext rc
            && !string.IsNullOrWhiteSpace(rc.CorrelationId.Value))
        {
            return rc.CorrelationId.Value;
        }

        return context.Request.Headers.TryGetValue("X-Correlation-ID", out var header)
            ? header.ToString()
            : Guid.NewGuid().ToString();
    }
}
