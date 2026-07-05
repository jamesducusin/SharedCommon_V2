# Testing Standards

See `.claude/skills/testing/SKILL.md` for task-level guidance.

## Test Types and Scope

| Type | What it tests | Speed | Database |
|------|--------------|-------|---------|
| Unit | Single class | Fast | No |
| Integration | Multiple classes + DI | Medium | Yes (real) |
| Architecture | Structural rules | Fast | No |
| Performance | Throughput/allocations | Slow | Optional |
| Security | Vulnerability patterns | Fast | No |

## Coverage Targets

- Unit tests: **≥ 80%** line coverage for all public APIs
- Integration tests: **happy path + 2 failure modes** per use case
- Architecture tests: **all layering and naming rules** enforced

## Naming Convention

```
[Method]_[Scenario]_[ExpectedOutcome]

GetOrder_WithValidId_ReturnsOrder
GetOrder_WithMissingId_ReturnsFailure
GetOrder_WhenDbUnavailable_ThrowsOperationException
```

## Test Project Structure

```
tests/
├── SharedCommon.UnitTests/
│   ├── [PackageName]/
│   │   ├── [ServiceName]Tests.cs
│   │   └── Fixtures/
├── SharedCommon.IntegrationTests/
│   ├── [PackageName]/
│   └── Infrastructure/  ← test DB setup, DI composition
├── SharedCommon.ArchitectureTests/
│   ├── LayeringTests.cs
│   ├── DependencyTests.cs
│   ├── NamingConventionTests.cs
│   ├── ObservabilityTests.cs
│   └── SecurityTests.cs
└── SharedCommon.PerformanceTests/
    └── BenchmarkTests.cs
```

## Test Utilities vs. Test Projects

**Test Utilities Library** (e.g., `src/SharedCommon.Testing`):
- Provides helpers, fakes, stubs for other test projects to consume
- Located in `src/` (not `tests/`), installed as a NuGet package in test projects
- **Must** set `<IsTestProject>false</IsTestProject>` in `.csproj` to prevent test runner discovery
- Consumed by: unit tests, integration tests, performance tests
- Examples: `TestCurrentUser`, `NullCurrentUser`, `TestRequestContext`, `ResultAssertions`, fake/in-memory implementations

**Test Projects** (e.g., `tests/SharedCommon.Core.UnitTests`):
- Located in `tests/` folder
- Contains actual test cases that run via `dotnet test`
- Do NOT set `<IsTestProject>false</IsTestProject>` (default is `true`)
- These get discovered by the test runner and executed

## DI in Tests

Always use constructor injection, never `new`:

```csharp
public class OrderServiceTests
{
    private readonly IOrderService _sut;
    private readonly IOrderRepository _repository;

    public OrderServiceTests()
    {
        _repository = Substitute.For<IOrderRepository>();
        _sut = new OrderService(_repository, NullLogger<OrderService>.Instance);
    }
}
```

## Integration Test Pattern

```csharp
public class OrderIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreated()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/orders", new CreateOrderRequest { ... });
        response.EnsureSuccessStatusCode();
    }
}
```

## Middleware Testing Pattern

**Setup**: Use `DefaultHttpContext` + `NSubstitute` for lightweight unit tests. Use `TestServer` for integration tests.

```csharp
private static HttpContext CreateHttpContext()
{
    var context = new DefaultHttpContext();
    context.Request.Method = "GET";
    context.Request.Path = "/";
    context.RequestServices = Substitute.For<IServiceProvider>();
    return context;
}

[Fact]
public async Task InvokeAsync_WithValidHeader_UsesHeader()
{
    // Arrange
    var context = CreateHttpContext();
    context.Request.Headers["X-Custom"] = "value";
    var middleware = new CustomMiddleware(_ => Task.CompletedTask);

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.Equal("value", context.Response.Headers["X-Custom"]);
}
```

**For response body**: Wrap in `MemoryStream` and deserialize:

```csharp
context.Response.Body = new MemoryStream();
var middleware = new ExceptionMiddleware(_ => throw new NotFoundException("msg"), options);
await middleware.InvokeAsync(context);

var body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
var json = JsonDocument.Parse(body);
```

## Forbidden in Tests

- `Thread.Sleep` — use `await Task.Delay` or control time
- Shared mutable state between tests
- External network calls (mock at boundary)
- Real database in unit tests
- Tests that depend on test execution order
- `async void` in tests (use `Task`-returning `[Fact]` instead)
