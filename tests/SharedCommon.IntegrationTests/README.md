# Integration Tests

## Strategy

Integration tests verify components working together with real infrastructure.
Uses real DI container, real database, real Redis (via docker-compose).

## Prerequisites

Start infrastructure before running:
```powershell
docker-compose -f infra/docker/docker-compose.yml up -d
```

## Running Tests

```powershell
dotnet test tests/SharedCommon.IntegrationTests
```

## Test Data Isolation

Each test creates and cleans up its own data. Tests must not share mutable state.

Use `IAsyncLifetime` for setup/teardown:

```csharp
public class OrderIntegrationTests : IAsyncLifetime
{
    public async Task InitializeAsync() { /* create test data */ }
    public async Task DisposeAsync() { /* clean up test data */ }
}
```

## Infrastructure

- Redis: `localhost:6379` (via docker-compose)
- Kafka: `localhost:9092` (via docker-compose)
- SQL: configured via `appsettings.test.json`

See: docs/standards/testing-standards.md
See: docs/runbooks/local-development.md
