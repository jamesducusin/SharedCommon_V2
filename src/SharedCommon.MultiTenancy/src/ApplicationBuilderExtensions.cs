using Microsoft.AspNetCore.Builder;

namespace SharedCommon.MultiTenancy;

/// <summary>Middleware pipeline extensions for SharedCommon.MultiTenancy.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the tenant resolution middleware to the pipeline.
    /// Place this before <c>UseAuthentication</c> so tenant context is available to auth handlers.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseSharedMultiTenancy(this IApplicationBuilder app)
        => app.UseMiddleware<TenantMiddleware>();
}
