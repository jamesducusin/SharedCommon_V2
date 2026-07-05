# Unit Tests

## Strategy

Unit tests verify individual classes in isolation. No I/O, no database, no network.

## Structure

```
SharedCommon.UnitTests/
├── Core/
│   ├── ResultTests.cs
│   └── GuardTests.cs
├── Logging/
│   └── CorrelationIdEnricherTests.cs
├── Caching/
│   ├── InMemoryCacheServiceTests.cs
│   └── CacheKeyBuilderTests.cs
└── [PackageName]/
    └── [ClassName]Tests.cs
```

## Running Tests

```powershell
dotnet test tests/SharedCommon.UnitTests
dotnet test tests/SharedCommon.UnitTests --collect:"XPlat Code Coverage"
```

## Coverage Target

≥ 80% line coverage for all public APIs.

## Test Conventions

- `[Method]_[Scenario]_[ExpectedOutcome]` naming
- AAA structure (Arrange, Act, Assert)
- One assertion concept per test
- Dependencies mocked via NSubstitute

See: docs/standards/testing-standards.md
