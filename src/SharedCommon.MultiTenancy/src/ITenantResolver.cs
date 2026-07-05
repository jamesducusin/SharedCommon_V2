using Microsoft.AspNetCore.Http;

namespace SharedCommon.MultiTenancy;

/// <summary>
/// Resolves the current tenant from an incoming HTTP request.
/// Implement this interface to add custom tenant resolution logic (e.g., database lookup).
///
/// <para>
/// Register a custom implementation via:
/// <code>
/// services.AddSharedMultiTenancy(config);
/// services.AddScoped&lt;ITenantResolver, MyCustomTenantResolver&gt;();
/// </code>
/// </para>
/// </summary>
/// <remarks>
/// <para><strong>Security Considerations:</strong></para>
/// <list type="bullet">
/// <item><description>
/// <strong>Input Validation:</strong> Tenant IDs come from untrusted sources.
/// Validate length (recommend ≤255 chars) and character set (alphanumeric + `-_` only).
/// </description></item>
/// <item><description>
/// <strong>Output Validation:</strong> After resolution, verify the tenant record is valid
/// before returning. Check for null, empty, or suspicious data.
/// </description></item>
/// <item><description>
/// <strong>Authorization Check:</strong> If performing database lookup, verify user
/// has permission to access the resolved tenant BEFORE returning.
/// </description></item>
/// <item><description>
/// <strong>Timing Attacks:</strong> Be aware that tenant resolution may leak information
/// about valid/invalid tenants via response time. Use constant-time comparisons where possible.
/// </description></item>
/// <item><description>
/// <strong>Cross-Request Isolation:</strong> Do not cache tenant resolution across requests.
/// Each request must re-resolve to prevent stale/incorrect tenant context.
/// </description></item>
/// </list>
/// </remarks>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant for the given HTTP request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="TenantInfo"/> if a tenant was resolved; <c>null</c> if the request
    /// has no tenant identity (anonymous or cross-tenant).
    /// </returns>
    Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken ct = default);
}
