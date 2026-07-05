# SharedCommon.Cloud

**NuGet package ID: `SharedCommon.Cloud.Abstractions`** (folder and namespace remain `SharedCommon.Cloud`).
Cloud provider abstractions: blob storage, secret manager, queues.
Supports Azure and AWS behind a unified interface.
Concrete implementations live in `SharedCommon.Cloud.Azure` and (future) `SharedCommon.Cloud.Aws`.

## API Surface

- `IBlobStorageService` — upload, download, delete blobs
- `ISecretManagerService` — read secrets from cloud provider
- `ICloudQueueService` — send/receive from cloud queues (SQS/Service Bus)
- `CloudOptions` — provider selection and configuration
- `AddSharedCloud(IConfiguration)` — DI registration

## Rules

**Must:**
- Use abstraction interfaces — never reference Azure/AWS SDK directly in consuming code
- Provider selected via configuration (`CloudOptions:Provider = "Azure" | "AWS"`)
- Retry policies applied to all cloud calls (use SharedCommon.Resiliency)
- Log cloud operation failures at Error with resource identifier

**Forbidden:**
- Hardcoded cloud resource names (bucket names, queue names, etc.)
- Accessing cloud resources without retry policy
- Storing cloud credentials in code (use SharedCommon.Security.ISecretProvider)

## Design Decisions

Provider abstraction allows tests to use local emulators (Azurite, LocalStack)
without changing application code.

## Test Strategy

- Unit tests mock `IBlobStorageService` etc.
- Integration tests use Azurite (Azure) or LocalStack (AWS) via docker-compose
- Test resilience by simulating provider failures

## Extension Points

- New cloud providers via `IBlobStorageService` implementation
- Register via `services.AddCloudProvider<AzureBlobService>()`
