namespace SharedCommon.FeatureFlags;

/// <summary>
/// Configuration for SharedCommon.FeatureFlags.
///
/// Feature definitions live under the standard Microsoft.FeatureManagement key:
/// <code>
/// {
///   "FeatureManagement": {
///     "NewCheckoutFlow": true,
///     "DarkMode": false,
///     "BetaDashboard": {
///       "EnabledFor": [
///         {
///           "Name": "Percentage",
///           "Parameters": { "Value": 20 }
///         }
///       ]
///     }
///   },
///   "SharedCommon": {
///     "FeatureFlags": {
///       "CacheTtlSeconds": 0,
///       "LogEvaluations": true
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class FeatureFlagOptions
{
    /// <summary>Configuration section path for SharedCommon overrides.</summary>
    public const string SectionName = "SharedCommon:FeatureFlags";

    /// <summary>
    /// Seconds to cache feature flag evaluation results.
    /// Set to 0 to disable caching (always evaluates against the source).
    /// Defaults to 0 (no caching).
    /// </summary>
    public int CacheTtlSeconds { get; init; } = 0;

    /// <summary>
    /// Emit a debug log entry for every feature flag evaluation.
    /// Useful during development. Defaults to <c>true</c>.
    /// </summary>
    public bool LogEvaluations { get; init; } = true;
}
