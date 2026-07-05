using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharedCommon.Cloud.Azure;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Uses <see cref="DefaultAzureCredential"/> when <c>UseManagedIdentity = true</c> (default).
/// Falls back to connection string when <c>UseManagedIdentity = false</c>.
/// </summary>
internal sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IOptions<CloudOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var azure = options.Value.Azure;

        if (azure.StorageAccountName is null)
            throw new InvalidOperationException(
                "CloudOptions:Azure:StorageAccountName is required for IBlobStorageService.");

        _client = azure.UseManagedIdentity
            ? new BlobServiceClient(
                new Uri($"https://{azure.StorageAccountName}.blob.core.windows.net"),
                new DefaultAzureCredential())
            : new BlobServiceClient(azure.StorageAccountName);
    }

    /// <inheritdoc/>
    public async Task<Uri> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            ct);

        _logger.LogDebug("Uploaded blob {BlobName} to container {Container}", blobName, containerName);
        return blob.Uri;
    }

    /// <inheritdoc/>
    public async Task<Stream?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default)
    {
        try
        {
            var blob = _client.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
        _logger.LogDebug("Deleted blob {BlobName} from container {Container}", blobName, containerName);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses a User Delegation Key when authenticated with <see cref="DefaultAzureCredential"/>,
    /// so no storage account key is required.
    /// </remarks>
    public async Task<Uri> GetPresignedUrlAsync(
        string containerName,
        string blobName,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var expiresOn = DateTimeOffset.UtcNow.Add(expiry);
        var delegationKey = await _client.GetUserDelegationKeyAsync(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            expiresOn,
            ct);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresOn
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var uriBuilder = new BlobUriBuilder(
            _client.GetBlobContainerClient(containerName).GetBlobClient(blobName).Uri)
        {
            Sas = sasBuilder.ToSasQueryParameters(delegationKey.Value, _client.AccountName)
        };

        return uriBuilder.ToUri();
    }
}
