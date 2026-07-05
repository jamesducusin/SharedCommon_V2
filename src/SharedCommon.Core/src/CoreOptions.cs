using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Core;

/// <summary>
/// Core platform configuration shared by all SharedCommon packages.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "Core": {
///       "ApplicationName": "OrderService",
///       "EnvironmentName": "Production",
///       "Version": "1.0.0"
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class CoreOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Core</c>.</summary>
    public const string SectionName = "SharedCommon:Core";

    /// <summary>
    /// Human-readable application name used in all logs and traces.
    /// Required. Maximum 50 characters.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ApplicationName is required.")]
    [MaxLength(50, ErrorMessage = "ApplicationName must be 50 characters or fewer.")]
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Deployment environment name.
    /// Valid values: <c>Development</c>, <c>Staging</c>, <c>Production</c>.
    /// Required.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "EnvironmentName is required.")]
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version string for observability dashboards.
    /// Required. Example: <c>1.2.3</c>.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Version is required.")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Allowed CORS origins for API endpoints.
    /// Optional. Empty by default (no cross-origin access).
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
