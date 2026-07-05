# SharedCommon.Cloud.Azure

Azure provider implementation of the `SharedCommon.Cloud.Abstractions` interfaces.
Registers `IBlobStorageService`, `ISecretManagerService`, and `ICloudQueueService` backed by Azure SDKs.
Uses `DefaultAzureCredential` — Managed Identity in production, Azure CLI/env vars in development.

## API Surface

- `AddSharedCloudAzure()` — registers all three Azure implementations (call after `AddSharedCloud`)
- `AzureBlobStorageService` → Azure Blob Storage
- `AzureSecretManagerService` → Azure Key Vault
- `AzureServiceBusQueueService` → Azure Service Bus (ReceiveAndDelete mode)

## Registration

```csharp
builder.Services
    .AddSharedCloud(builder.Configuration)   // binds CloudOptions
    .AddSharedCloudAzure();                  // wires Azure implementations
```

## Configuration

```json
{
  "SharedCommon:Cloud": {
    "Provider": "Azure",
    "Azure": {
      "StorageAccountName": "mystorageaccount",
      "KeyVaultUri": "https://my-vault.vault.azure.net/",
      "ServiceBusNamespace": "my-namespace.servicebus.windows.net",
      "UseManagedIdentity": true
    }
  }
}
```

## Rules

**Must:**
- `UseManagedIdentity: true` in all non-local environments (never connection strings in production)
- Store connection strings in Key Vault or environment variables — never in `appsettings.json`
- Assign the correct Azure RBAC roles: `Storage Blob Data Contributor`, `Key Vault Secrets User`, `Azure Service Bus Data Sender/Receiver`

**Forbidden:**
- Referencing Azure SDK types (`BlobClient`, `SecretClient`, etc.) outside this package
- Storing Azure credentials in code

## Design Notes

`AzureServiceBusQueueService` uses `ReceiveAndDelete` mode. Messages are deleted on receipt —
`AcknowledgeAsync` is a no-op. For PeekLock with explicit acknowledgement, implement a custom
`ICloudQueueService`.

`GetPresignedUrlAsync` uses a User Delegation Key (no storage account key required when using
Managed Identity). Requires the `Storage Blob Delegator` role on the storage account.

## Test Strategy

- Unit tests mock `IBlobStorageService`, `ISecretManagerService`, `ICloudQueueService`
- Integration tests use Azurite (Blob/Queue emulator) via `docker-compose`
- Key Vault integration tests require a real Key Vault or a mock (`Azure.Security.KeyVault.Secrets` test doubles)

## Extension Points

- Override any implementation by re-registering after `AddSharedCloudAzure`
- AWS implementations will follow the same pattern in `SharedCommon.Cloud.Aws`
