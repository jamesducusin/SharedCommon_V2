namespace SharedCommon.Cloud;

/// <summary>
/// Abstraction over cloud blob storage (Azure Blob Storage, AWS S3).
/// Implement per provider; application code only references this interface.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a stream to the specified container and blob name.
    /// </summary>
    /// <param name="containerName">Bucket or container name.</param>
    /// <param name="blobName">Path/key of the blob within the container.</param>
    /// <param name="content">Stream to upload.</param>
    /// <param name="contentType">MIME type of the content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>URI of the uploaded blob.</returns>
    Task<Uri> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a blob as a stream.
    /// </summary>
    /// <param name="containerName">Bucket or container name.</param>
    /// <param name="blobName">Blob path/key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The blob content as a stream, or <c>null</c> if not found.</returns>
    Task<Stream?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a blob. No-op if the blob does not exist.
    /// </summary>
    /// <param name="containerName">Bucket or container name.</param>
    /// <param name="blobName">Blob path/key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if the blob exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a pre-signed URL granting temporary read access.
    /// </summary>
    /// <param name="containerName">Bucket or container name.</param>
    /// <param name="blobName">Blob path/key.</param>
    /// <param name="expiry">How long the URL should remain valid.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Uri> GetPresignedUrlAsync(
        string containerName,
        string blobName,
        TimeSpan expiry,
        CancellationToken ct = default);
}
