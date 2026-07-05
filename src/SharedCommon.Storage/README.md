# SharedCommon.Storage

Provider-agnostic file storage abstraction. Includes a `LocalFileStorageService` for development and testing. Swap to a cloud-backed implementation via `SharedCommon.Cloud` without changing application code.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Storage
```

## Registration

```csharp
builder.Services.AddSharedStorage(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Storage": {
      "Provider": "Local",
      "LocalBasePath": "./storage",
      "ContainerName": "uploads"
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `Provider` | `Local` | `Local` uses the filesystem. `Cloud` wires `IBlobStorageService` from SharedCommon.Cloud. |
| `LocalBasePath` | `./storage` | Root directory for local storage. Created automatically on first write. |
| `ContainerName` | `default` | Logical namespace within the base path. Prevents collisions between features. |

---

## Usage

Inject `IFileStorageService` wherever file operations are needed:

```csharp
public class DocumentService(IFileStorageService storage)
{
    public async Task<string> UploadAsync(string fileName, Stream content, CancellationToken ct)
    {
        return await storage.SaveAsync($"documents/{fileName}", content, "application/pdf", ct);
    }

    public async Task<Stream?> DownloadAsync(string fileName, CancellationToken ct)
    {
        return await storage.ReadAsync($"documents/{fileName}", ct);
    }
}
```

All paths are **logical and relative to the container** — never pass absolute OS paths.

---

## API

```csharp
// Save a file — returns the logical path
string path = await storage.SaveAsync("invoices/inv-001.pdf", stream, "application/pdf", ct);

// Read — returns null if the file does not exist
Stream? content = await storage.ReadAsync("invoices/inv-001.pdf", ct);

// Check existence
bool exists = await storage.ExistsAsync("invoices/inv-001.pdf", ct);

// Delete — no-op if the file does not exist (never throws)
await storage.DeleteAsync("invoices/inv-001.pdf", ct);

// List files by prefix
IReadOnlyList<StorageFile> files = await storage.ListAsync("invoices/", ct);
foreach (var file in files)
    Console.WriteLine($"{file.Path} ({file.SizeBytes} bytes)");
```

---

## Cloud Backend

Register a cloud-backed implementation using `IBlobStorageService` from `SharedCommon.Cloud`:

```csharp
public class AzureBlobFileStorageService(IBlobStorageService blobs, IOptions<StorageOptions> opts)
    : IFileStorageService
{
    // ... delegate SaveAsync, ReadAsync, etc. to IBlobStorageService
}

// Register after AddSharedStorage to replace LocalFileStorageService:
builder.Services.AddSingleton<IFileStorageService, AzureBlobFileStorageService>();
```

---

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IFileStorageService` | Singleton | `LocalFileStorageService` by default. Replace with a cloud implementation for production. |
| `StorageOptions` | Singleton (Options) | Validated at startup. |
