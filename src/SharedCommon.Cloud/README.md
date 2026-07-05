# SharedCommon.Cloud

Cloud provider abstractions for blob storage, secret management, and cloud queues. Unified interface for Azure and AWS — application code never references provider SDKs directly.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Cloud
```

## Registration

```csharp
builder.Services.AddSharedCloud(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Cloud": {
      "Provider": "Azure",
      "Azure": {
        "StorageAccountName": "mystorageaccount",
        "KeyVaultUri": "https://my-vault.vault.azure.net/",
        "ServiceBusNamespace": "my-namespace.servicebus.windows.net",
        "UseManagedIdentity": true
      }
    }
  }
}
```

For AWS:
```json
{
  "SharedCommon": {
    "Cloud": {
      "Provider": "AWS",
      "Aws": {
        "Region": "us-east-1"
      }
    }
  }
}
```

> **Credentials** — never put `AccessKeyId`, `SecretAccessKey`, or connection strings in `appsettings.json`. Use Managed Identity (Azure) or IAM Roles (AWS) in production. Use User Secrets or environment variables locally.

| Property | Default | Notes |
|----------|---------|-------|
| `Provider` | _(required)_ | `Azure` or `AWS` |
| `Azure.UseManagedIdentity` | `true` | Prefer over connection strings |
| `Azure.KeyVaultUri` | `null` | Required for `ISecretManagerService` |
| `Azure.ServiceBusNamespace` | `null` | Required for `ICloudQueueService` |
| `Aws.Region` | `us-east-1` | Required |
| `Aws.ServiceUrl` | `null` | LocalStack override for local dev |

---

## Blob Storage

Inject `IBlobStorageService` into any service:

```csharp
public class DocumentService(IBlobStorageService blobs)
{
    public async Task<Uri> UploadAsync(Stream file, string fileName, CancellationToken ct)
    {
        return await blobs.UploadAsync(
            containerName: "documents",
            blobName: $"uploads/{fileName}",
            content: file,
            contentType: "application/pdf",
            ct: ct);
    }

    public async Task<Stream?> DownloadAsync(string fileName, CancellationToken ct) =>
        await blobs.DownloadAsync("documents", $"uploads/{fileName}", ct);
}
```

### Available Operations

| Method | Description |
|--------|-------------|
| `UploadAsync` | Upload a stream; returns the blob URI |
| `DownloadAsync` | Download as stream; null if not found |
| `DeleteAsync` | Delete a blob (no-op if not found) |
| `ExistsAsync` | Check if a blob exists |
| `GetPresignedUrlAsync` | Generate a time-limited pre-signed download URL |

---

## Secret Manager

```csharp
public class ApiClient(ISecretManagerService secrets)
{
    public async Task<string> GetTokenAsync(CancellationToken ct)
    {
        var token = await secrets.GetSecretAsync("api-token", ct);
        return token ?? throw new InvalidOperationException("Secret 'api-token' not found.");
    }
}
```

---

## Cloud Queue

```csharp
public class OrderProcessor(ICloudQueueService queue)
{
    public async Task SendAsync(OrderCreatedEvent evt, CancellationToken ct) =>
        await queue.SendAsync("orders", evt, ct: ct);

    public async Task ProcessAsync(CancellationToken ct)
    {
        var messages = await queue.ReceiveAsync<OrderCreatedEvent>("orders", maxMessages: 10, ct);
        foreach (var msg in messages)
        {
            await HandleAsync(msg.Payload, ct);
            await queue.AcknowledgeAsync("orders", msg.ReceiptHandle, ct);
        }
    }
}
```

---

## Implementing a Provider

Register an implementation of each interface when your provider package is installed:

```csharp
// In your Azure provider package:
services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
services.AddScoped<ISecretManagerService, AzureKeyVaultService>();
services.AddScoped<ICloudQueueService, AzureServiceBusQueueService>();
```

Use LocalStack or Azurite for local development to avoid real cloud costs.

---

## What Gets Registered

`AddSharedCloud` registers only the options and validates configuration at startup. Provider implementations must be registered separately by the corresponding provider package.

| Service | Notes |
|---------|-------|
| `CloudOptions` | Singleton (Options). Validated at startup. |
| `IBlobStorageService` | Registered by provider package |
| `ISecretManagerService` | Registered by provider package |
| `ICloudQueueService` | Registered by provider package |
