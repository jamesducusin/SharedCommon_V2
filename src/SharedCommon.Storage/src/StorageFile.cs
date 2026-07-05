namespace SharedCommon.Storage;

/// <summary>
/// Metadata for a file stored by <see cref="IFileStorageService"/>.
/// Does not hold file content — call <see cref="IFileStorageService.ReadAsync"/> to stream it.
/// </summary>
/// <param name="Name">File name without directory path.</param>
/// <param name="Path">Full logical path within the container.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="ContentType">MIME type (e.g., <c>image/png</c>).</param>
/// <param name="LastModified">UTC timestamp of last write.</param>
public sealed record StorageFile(
    string Name,
    string Path,
    long SizeBytes,
    string ContentType,
    DateTimeOffset LastModified);
