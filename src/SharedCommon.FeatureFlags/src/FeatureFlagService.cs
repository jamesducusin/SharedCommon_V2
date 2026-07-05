using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace SharedCommon.FeatureFlags;

/// <summary>
/// Default implementation of <see cref="IFeatureFlagService"/> backed by
/// <see cref="IFeatureManager"/> from Microsoft.FeatureManagement.
/// </summary>
internal sealed class FeatureFlagService(
    IFeatureManager featureManager,
    ILogger<FeatureFlagService> logger) : IFeatureFlagService
{
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        var enabled = await featureManager.IsEnabledAsync(featureName);
        logger.LogDebug("Feature flag '{FeatureName}' evaluated: {Enabled}", featureName, enabled);
        return enabled;
    }

    public async Task<bool> IsEnabledForAsync<TContext>(
        string featureName,
        TContext context,
        CancellationToken ct = default)
    {
        var enabled = await featureManager.IsEnabledAsync(featureName, context);
        logger.LogDebug(
            "Feature flag '{FeatureName}' evaluated for context {ContextType}: {Enabled}",
            featureName, typeof(TContext).Name, enabled);
        return enabled;
    }

    public async Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(CancellationToken ct = default)
    {
        var enabled = new List<string>();
        await foreach (var feature in featureManager.GetFeatureNamesAsync())
        {
            if (await featureManager.IsEnabledAsync(feature))
                enabled.Add(feature);
        }
        return enabled;
    }
}
