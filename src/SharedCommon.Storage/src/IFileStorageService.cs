namespace SharedCommon.Storage;

/// <summary>
/// Provider-agnostic file storage service. Implementations include local filesystem
/// and cloud-delegating providers.
///
/// <para>
/// All paths are logical and relative to the configured container — never absolute OS paths.
/// </para>
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves content to the specified path, creating parent directories as needed.
    /// Overwrites existing content.
    /// </summary>
    /// <param name="path">Logical file path within the container (e.g., <c>avatars/user-123.jpg</c>).</param>
    /// <param name="content">File content stream.</param>
    /// <param name="contentType">MIME type of the content. Defaults to <c>application/octet-stream</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The final storage path.</returns>
    Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken ct = default);

    /// <summary>
    /// Opens a read-only stream for the file at the given path.
    /// </summary>
    /// <param name="path">Logical file path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readable stream, or <c>null</c> if the file does not exist.</returns>
    Task<Stream?> ReadAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Deletes the file at the given path. No-op if the file does not exist.
    /// </summary>
    /// <param name="path">Logical file path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if a file exists at the given path.
    /// </summary>
    /// <param name="path">Logical file path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Lists files under the given prefix within the container.
    /// </summary>
    /// <param name="prefix">Path prefix to filter by. Empty string lists all files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata records for all matching files.</returns>
    Task<IReadOnlyList<StorageFile>> ListAsync(string prefix = "", CancellationToken ct = default);
}
