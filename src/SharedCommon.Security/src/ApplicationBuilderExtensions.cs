using Microsoft.AspNetCore.Builder;

namespace SharedCommon.Security;

/// <summary>
/// <see cref="IApplicationBuilder"/> extension methods for SharedCommon security middleware.
///
/// Recommended ordering in Program.cs:
/// <code>
/// app.UseSharedCommonSecurityHeaders(); // First — always set headers
/// app.UseHttpsRedirection();
/// app.UseSharedCommonRateLimit();
/// app.UseAuthentication();
/// app.UseAuthorization();
/// </code>
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="SecurityHeadersMiddleware"/> to the pipeline.
    /// Writes OWASP-recommended HTTP security headers (HSTS, CSP, X-Frame-Options, etc.).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseSharedCommonSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
