namespace SharedCommon.MultiTenancy;

/// <summary>
/// Resolved tenant identity. Populated by <see cref="ITenantResolver"/> on each request.
/// </summary>
/// <param name="TenantId">Unique tenant identifier. Never null or empty.</param>
/// <param name="TenantName">Optional human-readable tenant name.</param>
/// <param name="Properties">Arbitrary additional metadata for this tenant.</param>
public sealed record TenantInfo(
    string TenantId,
    string? TenantName = null,
    IReadOnlyDictionary<string, string>? Properties = null);
