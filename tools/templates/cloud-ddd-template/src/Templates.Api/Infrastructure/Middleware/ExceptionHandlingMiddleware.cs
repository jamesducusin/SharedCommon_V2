namespace Templates.Api.Infrastructure.Middleware;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Templates.Api.Common.Models;

/// <summary>
/// Custom exception handler middleware that maps domain exceptions and other errors
/// to standardized ApiErrorResponse format.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var traceId = context.TraceIdentifier;
        var timestamp = DateTime.UtcNow;

        ApiErrorResponse errorResponse;

        // Check if exception is a domain exception by type name
        // (avoiding direct reference to DomainException base class)
        var exceptionType = exception.GetType();
        var isDomainException = exceptionType.Name.EndsWith("Exception") &&
                               exceptionType.BaseType?.Name == "DomainException";

        if (isDomainException)
        {
            // Extract error code and status code from domain exception
            var errorCodeProperty = exceptionType.GetProperty("ErrorCode");
            var statusCodeProperty = exceptionType.GetProperty("StatusCode");
            var detailsProperty = exceptionType.GetProperty("Details");

            var errorCode = (string?)errorCodeProperty?.GetValue(exception) ?? "DOMAIN_ERROR";
            var statusCode = (int?)statusCodeProperty?.GetValue(exception) ?? 400;
            var details = (Dictionary<string, object>?)detailsProperty?.GetValue(exception);

            context.Response.StatusCode = statusCode;
            errorResponse = new ApiErrorResponse(
                TraceId: traceId,
                StatusCode: statusCode,
                Error: new ErrorDetail(
                    Code: errorCode,
                    Message: exception.Message,
                    Details: details),
                Timestamp: timestamp);
        }
        // Check for validation exception by type name
        else if (exceptionType.Name == "ValidationException" &&
                exceptionType.Namespace?.StartsWith("FluentValidation") == true)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            
            // Try to extract validation errors
            var errorsProperty = exceptionType.GetProperty("Errors");
            var validationErrors = new Dictionary<string, object>();
            
            if (errorsProperty?.GetValue(exception) is System.Collections.IEnumerable errors)
            {
                foreach (var error in errors)
                {
                    var propName = error.GetType().GetProperty("PropertyName")?.GetValue(error) as string;
                    var message = error.GetType().GetProperty("ErrorMessage")?.GetValue(error) as string;
                    
                    if (!string.IsNullOrEmpty(propName))
                    {
                        if (!validationErrors.ContainsKey(propName))
                            validationErrors[propName] = new List<string>();
                        
                        if (validationErrors[propName] is List<string> messages && !string.IsNullOrEmpty(message))
                            messages.Add(message);
                    }
                }
            }

            var details = new Dictionary<string, object> { { "validationErrors", validationErrors } };

            errorResponse = new ApiErrorResponse(
                TraceId: traceId,
                StatusCode: StatusCodes.Status400BadRequest,
                Error: new ErrorDetail(
                    Code: "VALIDATION_FAILED",
                    Message: "One or more validation errors occurred",
                    Details: details),
                Timestamp: timestamp);
        }
        // Default to 500 Internal Server Error
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            errorResponse = new ApiErrorResponse(
                TraceId: traceId,
                StatusCode: StatusCodes.Status500InternalServerError,
                Error: new ErrorDetail(
                    Code: "INTERNAL_ERROR",
                    Message: "An internal error occurred. Please contact support with the trace ID.",
                    Details: new Dictionary<string, object> { { "traceId", traceId } }),
                Timestamp: timestamp);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        return context.Response.WriteAsJsonAsync(errorResponse, options);
    }
}

/// <summary>
/// Extension methods for exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds custom exception handling middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
