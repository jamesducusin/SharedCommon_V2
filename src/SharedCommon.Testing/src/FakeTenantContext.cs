using SharedCommon.MultiTenancy;

namespace SharedCommon.Testing;

/// <summary>
/// Configurable <see cref="ITenantContext"/> for unit tests.
/// Resolved by default — use <see cref="Unresolved"/> for testing the no-tenant path.
/// </summary>
public sealed class FakeTenantContext : ITenantContext
{
    /// <inheritdoc />
    public string TenantId { get; set; } = "test-tenant";

    /// <inheritdoc />
    public string? TenantName { get; set; } = "Test Tenant";

    /// <inheritdoc />
    public bool IsResolved { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Properties { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns an unresolved tenant context for testing guards that require a tenant.</summary>
    public static FakeTenantContext Unresolved => new() { IsResolved = false, TenantId = string.Empty, TenantName = null };

    /// <summary>Creates a resolved context for a specific tenant.</summary>
    public static FakeTenantContext For(string tenantId, string? tenantName = null) =>
        new() { TenantId = tenantId, TenantName = tenantName ?? tenantId };
}
