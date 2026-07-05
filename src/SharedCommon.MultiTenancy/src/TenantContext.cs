namespace SharedCommon.MultiTenancy;

/// <summary>
/// Mutable, scoped implementation of <see cref="ITenantContext"/>.
/// Set by <see cref="TenantMiddleware"/> after <see cref="ITenantResolver"/> resolves the tenant.
/// </summary>
internal sealed class TenantContext : ITenantContext
{
    private static readonly IReadOnlyDictionary<string, string> EmptyProperties =
        new Dictionary<string, string>();

    public string TenantId { get; private set; } = string.Empty;
    public string? TenantName { get; private set; }
    public bool IsResolved { get; private set; }
    public IReadOnlyDictionary<string, string> Properties { get; private set; } = EmptyProperties;

    internal void SetTenant(TenantInfo info)
    {
        TenantId = info.TenantId;
        TenantName = info.TenantName;
        IsResolved = true;
        Properties = info.Properties ?? EmptyProperties;
    }
}
