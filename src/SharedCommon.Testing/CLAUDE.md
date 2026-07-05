# SharedCommon.Testing

Test helper package for SharedCommon consumers. Install in test projects only.
Provides fake/stub implementations of SharedCommon interfaces and xUnit assertion extensions.

**Configuration Note**: `.csproj` includes `<IsTestProject>false</IsTestProject>` to prevent this utility library from being discovered as a test suite by the test runner. This is a test utility library, not a test project.

## API Surface

| Type | Purpose |
|------|---------|
| `TestRequestContext` | Configurable `IRequestContext` with deterministic correlation ID |
| `FakeTenantContext` | Configurable `ITenantContext`; `FakeTenantContext.Unresolved` for no-tenant tests |
| `NullCurrentUser` | Anonymous `ICurrentUser` (unauthenticated) |
| `TestCurrentUser` | Authenticated `ICurrentUser` with configurable roles |
| `InMemoryAuditStore` | Thread-safe `IAuditStore`; inspect `.Entries` to assert audit events |
| `ResultAssertions` | xUnit extensions: `.ShouldSucceed()`, `.ShouldFail("CODE")`, `.ShouldBeInvalid("field")` |

## Usage

```csharp
// Service under test
var sut = new OrderService(
    currentUser: new TestCurrentUser("user-42", roles: "Customer"),
    tenantCtx:   FakeTenantContext.For("tenant-abc"),
    requestCtx:  new TestRequestContext { UserId = "user-42" },
    auditStore:  auditStore);

// Act
var result = await sut.PlaceOrderAsync(command, CancellationToken.None);

// Assert with ResultAssertions
var order = result.ShouldSucceed();
Assert.Equal("user-42", order.UserId);

// Assert audit was recorded
Assert.Single(auditStore.Entries.Where(e => e.Action == AuditAction.Created));
```

## Rules

- Install only in test projects — never in application code
- Prefer `NullCurrentUser.Instance` over `new NullCurrentUser()` to avoid allocations
- Call `InMemoryAuditStore.Clear()` in `IAsyncLifetime.InitializeAsync` to reset state between tests

## Extension Points

- Subclass `TestCurrentUser` or `FakeTenantContext` for domain-specific test scenarios
- Add domain-specific `ResultAssertions` extensions in your own test helper project
