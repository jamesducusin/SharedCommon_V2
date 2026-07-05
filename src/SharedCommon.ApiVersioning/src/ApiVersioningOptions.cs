using System.ComponentModel.DataAnnotations;

namespace SharedCommon.ApiVersioning;

/// <summary>
/// Configuration for the SharedCommon API versioning infrastructure.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "ApiVersioning": {
///       "DefaultVersion": "1.0",
///       "AssumeDefaultWhenUnspecified": true,
///       "ReportApiVersions": true
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class ApiVersioningOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:ApiVersioning";

    /// <summary>
    /// Default API version applied when a request does not specify one.
    /// Format: "MAJOR.MINOR" (e.g., "1.0"). Defaults to "1.0".
    /// </summary>
    [Required]
    public string DefaultVersion { get; init; } = "1.0";

    /// <summary>
    /// Whether to use <see cref="DefaultVersion"/> when the caller does not specify a version.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool AssumeDefaultWhenUnspecified { get; init; } = true;

    /// <summary>
    /// Include <c>api-supported-versions</c> and <c>api-deprecated-versions</c> in response headers.
    /// Useful for clients to discover available versions. Defaults to <c>true</c>.
    /// </summary>
    public bool ReportApiVersions { get; init; } = true;

    /// <summary>
    /// Strategy for reading the requested version from incoming requests.
    /// Multiple readers are evaluated in order — the first match wins.
    /// </summary>
    public VersionReadingStrategy Strategy { get; init; } = new();
}

/// <summary>
/// Controls where the API version is read from in a request.
/// Multiple sources are combined and evaluated in order.
/// </summary>
public sealed class VersionReadingStrategy
{
    /// <summary>
    /// Read version from URL path segment (e.g., <c>/api/v1/orders</c>).
    /// This is the recommended strategy. Defaults to <c>true</c>.
    /// </summary>
    public bool UrlSegment { get; init; } = true;

    /// <summary>
    /// Read version from a query string parameter (e.g., <c>?api-version=1.0</c>).
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool QueryString { get; init; } = false;

    /// <summary>
    /// Query string parameter name when <see cref="QueryString"/> is <c>true</c>.
    /// Default: <c>api-version</c>.
    /// </summary>
    public string QueryStringParameterName { get; init; } = "api-version";

    /// <summary>
    /// Read version from a request header (e.g., <c>X-Api-Version: 2.0</c>).
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool Header { get; init; } = false;

    /// <summary>
    /// Header name when <see cref="Header"/> is <c>true</c>.
    /// Default: <c>X-Api-Version</c>.
    /// </summary>
    public string HeaderName { get; init; } = "X-Api-Version";

    /// <summary>
    /// Read version from a media type parameter (e.g., <c>Accept: application/json;v=2.0</c>).
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool MediaType { get; init; } = false;
}
