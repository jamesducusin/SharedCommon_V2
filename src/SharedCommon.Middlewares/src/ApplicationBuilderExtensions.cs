using Microsoft.AspNetCore.Builder;

namespace SharedCommon.Middlewares;

/// <summary>
/// <see cref="IApplicationBuilder"/> extension methods for SharedCommon pipeline middleware.
///
/// Recommended ordering in Program.cs (matches ASP.NET Core pipeline conventions):
/// <code>
/// app.UseSharedCommonExceptionHandling(); // Must be first
/// app.UseSharedCommonCorrelationId();
/// app.UseSharedCommonRequestLogging();
/// app.UseRouting();
/// app.UseAuthentication();
/// app.UseAuthorization();
/// app.MapControllers();
/// </code>
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="ExceptionHandlingMiddleware"/> to the pipeline.
    /// Catches all unhandled exceptions and returns a standardized JSON error response.
    /// Must be registered first so it wraps all subsequent middleware.
    /// </summary>
    public static IApplicationBuilder UseSharedCommonExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// Adds <see cref="CorrelationIdMiddleware"/> to the pipeline.
    /// Ensures every request has a correlation ID available in headers and <c>IRequestContext</c>.
    /// </summary>
    public static IApplicationBuilder UseSharedCommonCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Adds <see cref="RequestLoggingMiddleware"/> to the pipeline.
    /// Logs method, path, status code, and duration for every non-excluded request.
    /// </summary>
    public static IApplicationBuilder UseSharedCommonRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
