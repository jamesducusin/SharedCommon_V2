# SharedCommon.Storage

Provider-agnostic file storage abstraction. Local filesystem implementation included.

## API Surface

- `IFileStorageService` — `SaveAsync`, `ReadAsync`, `DeleteAsync`, `ExistsAsync`, `ListAsync`
- `StorageFile` — file metadata record (Name, Path, SizeBytes, ContentType, LastModified)
- `StorageOptions` — `Provider` (Local/Cloud), `LocalBasePath`, `ContainerName`
- `AddSharedStorage(IConfiguration)` — registers `IFileStorageService`

## Rules

**Must:**
- All paths are logical, relative to the container — never absolute OS paths
- SaveAsync must create parent directories if they do not exist
- ListAsync must return an empty list (not throw) when container does not exist
- DeleteAsync must be a no-op (not throw) when the file does not exist
- Log all write operations at Debug level with the path

**Forbidden:**
- Absolute OS paths in the `path` parameter of any method
- Exposing provider-specific types (FileInfo, BlobClient) to callers
- Throwing exceptions for missing files on Read/Delete/Exists

## Design Decisions

`LocalFileStorageService` is `internal sealed` — registered via DI, not instantiated directly.
Cloud provider wires `IBlobStorageService` from `SharedCommon.Cloud` — no SDK references here.
`ContainerName` creates a logical namespace so the same `IFileStorageService` can serve multiple features
with different prefixes.

## Test Strategy

- Unit test `StorageOptions` defaults and `StorageFile` record
- Unit test `LocalFileStorageService` with a temp directory
- Test: save → read → exists → delete → exists=false lifecycle
- Test `ListAsync` prefix filtering

## Extension Points

- Implement `IFileStorageService` backed by Azure Blob via `SharedCommon.Cloud.IBlobStorageService`
- Register: `services.AddSingleton<IFileStorageService, AzureBlobFileStorageService>()`
