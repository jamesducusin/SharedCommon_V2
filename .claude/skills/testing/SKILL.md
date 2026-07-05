# Testing Skill

Design unit, integration, and architecture tests.

## When to Use This Skill

Triggers:
- Designing tests for a new feature
- Setting up integration test infrastructure
- Writing architecture tests to enforce constraints
- Creating test data fixtures

Ask Claude explicitly: "Use testing skill"

## Input (What You Provide)

- Feature or module to test
- Type of test needed (unit/integration/architecture)

## Output (What You Get)

- Test structure and class layout
- Fixture setup
- Edge cases to cover

## Checklist

**Unit Tests:**
- [ ] AAA structure (Arrange, Act, Assert)
- [ ] One assertion concept per test
- [ ] Dependencies mocked via DI
- [ ] No real I/O (database, network, file system)
- [ ] Descriptive test names (Method_Scenario_ExpectedResult)
- [ ] Edge cases: null, empty, boundary values

**Integration Tests:**
- [ ] Uses real DI container
- [ ] Uses real database (or test double at boundary)
- [ ] Isolated test data (no cross-test contamination)
- [ ] Cleanup after each test
- [ ] Tests full stack from service to persistence

**Architecture Tests (NetArchTest):**
- [ ] Layering rules enforced
- [ ] Dependency direction verified
- [ ] Naming conventions checked
- [ ] Forbidden patterns detected
- [ ] Observability requirements verified

**Test Quality:**
- [ ] Tests are deterministic (no random, no DateTime.Now)
- [ ] Tests are independent (no shared mutable state)
- [ ] Tests document behavior, not implementation
- [ ] Coverage targets met (>70% for new code)

## Test Naming Convention

```
[MethodName]_[Scenario]_[ExpectedResult]

Examples:
ProcessOrder_WithValidOrder_ReturnsSuccess
ProcessOrder_WithNullOrder_ThrowsArgumentNullException
ProcessOrder_WhenInventoryEmpty_ReturnsOutOfStockError
```

## Fixture Pattern

```csharp
public class OrderServiceTests : IClassFixture<ServiceFixture>
{
    private readonly IOrderService _sut;

    public OrderServiceTests(ServiceFixture fixture)
    {
        _sut = fixture.CreateOrderService();
    }

    [Fact]
    public async Task ProcessOrder_WithValidOrder_ReturnsSuccess()
    {
        // Arrange
        var order = OrderBuilder.Valid().Build();

        // Act
        var result = await _sut.ProcessAsync(order);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
```

## Common Mistakes

❌ Testing implementation details (private method calls)
- Why: Tests break on refactoring without behavior change
- Fix: Test observable behavior only

❌ Shared mutable state between tests
- Why: Test order dependency, flaky failures
- Fix: Create fresh instances per test

❌ `Thread.Sleep` in tests
- Why: Slow, unreliable
- Fix: Use `await Task.Delay` or control time with abstraction

## References

See: docs/standards/testing-standards.md
See: tests/SharedCommon.ArchitectureTests/
See: tests/SharedCommon.UnitTests/README.md
