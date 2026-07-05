namespace SharedCommon.FeatureFlags;

/// <summary>
/// High-level feature flag evaluation service. Wraps Microsoft.FeatureManagement
/// with a SharedCommon-idiomatic API.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Returns <c>true</c> if the named feature flag is enabled.
    /// </summary>
    /// <param name="featureName">Feature flag name as defined in configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if the named feature flag is enabled for the given context
    /// (e.g., a specific user or tenant).
    /// </summary>
    /// <typeparam name="TContext">Context type consumed by a contextual feature filter.</typeparam>
    /// <param name="featureName">Feature flag name.</param>
    /// <param name="context">Evaluation context (e.g., userId, tenantId).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsEnabledForAsync<TContext>(
        string featureName,
        TContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all active (enabled) feature flag names.
    /// Useful for diagnostics and dashboards.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(CancellationToken ct = default);
}

/// <summary>
/// Context for user-based or tenant-based feature flag targeting.
/// Pass to <see cref="IFeatureFlagService.IsEnabledForAsync{TContext}"/> when evaluating
/// targeting filters.
/// </summary>
public sealed record FeatureFlagContext(
    string? UserId = null,
    string? TenantId = null,
    IReadOnlyList<string>? Groups = null);
