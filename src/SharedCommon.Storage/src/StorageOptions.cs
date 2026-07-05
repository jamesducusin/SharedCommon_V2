using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Storage;

/// <summary>Storage provider selection.</summary>
public enum StorageProvider
{
    /// <summary>Local file system (default; suitable for development and single-node deployments).</summary>
    Local,

    /// <summary>Delegate to <c>IBlobStorageService</c> from <c>SharedCommon.Cloud</c>.</summary>
    Cloud
}

/// <summary>
/// Configuration for the SharedCommon file storage abstraction.
///
/// <code>
/// {
///   "SharedCommon": {
///     "Storage": {
///       "Provider": "Local",
///       "LocalBasePath": "./storage",
///       "ContainerName": "uploads"
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class StorageOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Storage";

    /// <summary>
    /// Active storage provider. Defaults to <see cref="StorageProvider.Local"/>.
    /// </summary>
    [Required]
    public StorageProvider Provider { get; init; } = StorageProvider.Local;

    /// <summary>
    /// Root directory for the local file system provider.
    /// Ignored when <see cref="Provider"/> is <see cref="StorageProvider.Cloud"/>.
    /// Defaults to <c>./storage</c>.
    /// </summary>
    public string LocalBasePath { get; init; } = "./storage";

    /// <summary>
    /// Logical container (folder or blob container) to scope all operations.
    /// Defaults to <c>default</c>.
    /// </summary>
    public string ContainerName { get; init; } = "default";
}
