using System.ComponentModel.DataAnnotations;

namespace SharedCommon.MultiTenancy;

/// <summary>Strategy for resolving the current tenant from an incoming request.</summary>
public enum TenantResolutionStrategy
{
    /// <summary>Read tenant ID from a request header (default).</summary>
    Header,

    /// <summary>Read tenant ID from a JWT claim.</summary>
    Claim,

    /// <summary>Extract tenant ID from the request subdomain (e.g., <c>tenant.api.example.com</c>).</summary>
    Subdomain,

    /// <summary>Read tenant ID from a query string parameter.</summary>
    QueryString
}

/// <summary>
/// Configuration for the SharedCommon multi-tenancy middleware.
///
/// <code>
/// {
///   "SharedCommon": {
///     "MultiTenancy": {
///       "Enabled": true,
///       "Strategy": "Header",
///       "HeaderName": "X-Tenant-Id",
///       "RequireTenant": false
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class MultiTenancyOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:MultiTenancy";

    /// <summary>Enable or disable multi-tenancy resolution. Defaults to <c>true</c>.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Strategy used to resolve the tenant from each request.
    /// Defaults to <see cref="TenantResolutionStrategy.Header"/>.
    /// </summary>
    [Required]
    public TenantResolutionStrategy Strategy { get; init; } = TenantResolutionStrategy.Header;

    /// <summary>
    /// HTTP header name to read the tenant ID from when using <see cref="TenantResolutionStrategy.Header"/>.
    /// Default: <c>X-Tenant-Id</c>.
    /// </summary>
    public string HeaderName { get; init; } = "X-Tenant-Id";

    /// <summary>
    /// JWT claim name to read the tenant ID from when using <see cref="TenantResolutionStrategy.Claim"/>.
    /// Default: <c>tenant_id</c>.
    /// </summary>
    public string ClaimName { get; init; } = "tenant_id";

    /// <summary>
    /// Query string parameter name when using <see cref="TenantResolutionStrategy.QueryString"/>.
    /// Default: <c>tenantId</c>.
    /// </summary>
    public string QueryStringKey { get; init; } = "tenantId";

    /// <summary>
    /// When <c>true</c>, requests without a resolvable tenant return <c>400 Bad Request</c>.
    /// When <c>false</c>, unresolved requests proceed with no tenant context. Default: <c>false</c>.
    /// </summary>
    public bool RequireTenant { get; init; } = false;
}
