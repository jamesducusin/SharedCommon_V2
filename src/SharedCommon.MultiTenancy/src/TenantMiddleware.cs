using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharedCommon.MultiTenancy;

/// <summary>
/// Middleware that resolves the current tenant on each request using <see cref="ITenantResolver"/>
/// and populates <see cref="ITenantContext"/> for downstream use.
/// </summary>
/// <remarks>
/// <para><strong>⚠️ Data Isolation:</strong></para>
/// <para>
/// This middleware only IDENTIFIES the tenant; it does NOT enforce data isolation.
/// Application code must enforce boundaries at query, cache, and authorization layers.
/// See <see cref="ITenantContext"/> remarks for complete data isolation requirements.
/// </para>
/// <para>
/// <strong>Security Notes:</strong>
/// <list type="bullet">
/// <item><description>Tenant IDs come from untrusted sources (headers, claims, subdomains). All downstream code must validate.</description></item>
/// <item><description>This context is <c>scoped</c> per request; it MUST NOT escape the request boundary.</description></item>
/// <item><description>Always enforce WHERE clause filtering in queries using the resolved TenantId.</description></item>
/// <item><description>For multi-instance deployments, tenant IDs must be stable and globally unique.</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class TenantMiddleware(
    RequestDelegate next,
    IOptions<MultiTenancyOptions> options,
    ILogger<TenantMiddleware> logger)
{
    private readonly MultiTenancyOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, ITenantContext tenantContext)
    {
        if (!_options.Enabled)
        {
            await next(context);
            return;
        }

        var info = await resolver.ResolveAsync(context);

        if (info is not null)
        {
            ((TenantContext)tenantContext).SetTenant(info);
            logger.LogDebug("Tenant resolved: {TenantId}", info.TenantId);
        }
        else if (_options.RequireTenant)
        {
            logger.LogWarning("Tenant resolution required but no tenant found in request.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant identification is required.");
            return;
        }

        await next(context);
    }
}
