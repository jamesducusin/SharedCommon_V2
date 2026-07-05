namespace SharedCommon.MultiTenancy;

/// <summary>
/// Request-scoped tenant context. Populated by the multi-tenancy middleware from
/// <see cref="ITenantResolver"/> and available via DI throughout the request lifetime.
/// </summary>
/// <remarks>
/// <para><strong>Data Isolation Warning:</strong></para>
/// <para>
/// This interface provides the current tenant identifier for the executing request.
/// <strong>CRITICAL:</strong> Applications must enforce data isolation at ALL layers:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>Query Layer:</strong> All database queries MUST include a WHERE clause filtering
/// to the current tenant. Use <see cref="TenantId"/> to filter in every repository method.
/// Failure to do so will cause data leakage between tenants.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Cache Layer:</strong> If using Redis or distributed cache, MUST include
/// <see cref="TenantId"/> as part of the cache key (e.g., <c>tenant:{TenantId}:resource:id</c>).
/// Cache hits from one tenant must never be served to another tenant.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Authorization:</strong> Do not rely solely on <see cref="TenantId"/> for authorization.
/// Always verify the user's role and permissions within the tenant context.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Audit &amp; Logging:</strong> Include <see cref="TenantId"/> in all audit logs,
/// correlation IDs, and metrics for traceability and forensics.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Third-Party APIs:</strong> When calling external services, ensure no tenant data
/// from one tenant leaks to another in response handling or error messages.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Static State:</strong> FORBIDDEN: Never store tenant-specific data in static fields,
/// caches without tenant keys, or singleton services without explicit tenant scoping.
/// </description>
/// </item>
/// </list>
/// <para>
/// <see cref="TenantId"/> is populated from untrusted user input (headers, claims, subdomains).
/// Always validate tenant ID length and character set. Use a whitelist of known tenant IDs where possible.
/// </para>
/// <para>
/// This context is <c>scoped</c> per request. It is UNSAFE to leak this reference into
/// background jobs, async operations outside the request, or between requests.
/// </para>
/// </remarks>
public interface ITenantContext
{
    /// <summary>Current tenant identifier. Empty string when no tenant is resolved.</summary>
    /// <remarks>Treat as untrusted user input. Always validate before use.</remarks>
    string TenantId { get; }

    /// <summary>Current tenant name, or <c>null</c> when unavailable.</summary>
    string? TenantName { get; }

    /// <summary><c>true</c> when a tenant has been successfully resolved for this request.</summary>
    bool IsResolved { get; }

    /// <summary>Additional metadata about the current tenant.</summary>
    IReadOnlyDictionary<string, string> Properties { get; }
}
