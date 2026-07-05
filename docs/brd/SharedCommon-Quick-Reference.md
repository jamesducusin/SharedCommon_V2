# SharedCommon Quick Reference for Claude Code

**Use this when implementing any module.**

---

## Module Implementation Checklist

Every module MUST include:

### Structure
- [ ] `src/[ModuleName]/` folder created
- [ ] CLAUDE.md with module-specific rules
- [ ] README.md with examples
- [ ] `.csproj` with proper NuGet metadata

### Public API
- [ ] Interfaces for all public contracts
- [ ] ServiceCollectionExtensions with Add[ModuleName]()
- [ ] 100% XML documentation on all public members
- [ ] Usage example in README

### Configuration
- [ ] IOptions<[Options]> class created
- [ ] Configuration schema documented in BRD
- [ ] Validation at startup (ValidateOnStart)
- [ ] Sensible defaults for every property
- [ ] No hardcoded values (all externalized)

### Error Handling
- [ ] Try-catch around external calls
- [ ] Proper logging with context
- [ ] Graceful degradation where possible
- [ ] Specific exception handling (not catch-all)

### Testing
- [ ] Unit tests in `tests/[ModuleName].UnitTests/`
- [ ] Integration tests in `tests/[ModuleName].IntegrationTests/`
- [ ] Mock external dependencies
- [ ] Test error scenarios
- [ ] >70% coverage minimum

### Security
- [ ] No secrets in code
- [ ] Input validation where needed
- [ ] Secure defaults
- [ ] No credential logging

### Observability
- [ ] ILogger injected in services
- [ ] Key operations logged
- [ ] Structured properties used
- [ ] Correlation ID propagated

---

## API Surface Template

```csharp
namespace SharedCommon.[ModuleName];

/// <summary>
/// [Concise description of what this does].
/// </summary>
public interface I[ServiceName]
{
    /// <summary>
    /// [What the method does].
    /// 
    /// Example:
    /// <code>
    /// var result = await service.MethodAsync("input", ct);
    /// </code>
    /// </summary>
    /// <param name="parameter">[What this is]</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>[What it returns and when]</returns>
    /// <exception cref="ArgumentException">[When thrown]</exception>
    Task<Result<T>> MethodAsync(
        string parameter,
        CancellationToken ct = default);
}

/// <summary>Registration extension.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add [ModuleName] infrastructure.
    /// 
    /// Configures:
    /// - [What is configured]
    /// - [What is configured]
    /// </summary>
    public static IServiceCollection Add[ModuleName](
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("SharedCommon:[ModuleName]")
            .Get<[Options]>() 
            ?? throw new InvalidOperationException(
                "SharedCommon:[ModuleName] configuration required");
        
        services.Configure<[Options]>(
            configuration.GetSection("SharedCommon:[ModuleName]"));
        
        services.AddScoped<I[ServiceName], [ServiceName]>();
        
        return services;
    }
}
```

---

## Configuration Options Template

```csharp
namespace SharedCommon.[ModuleName].Configuration;

/// <summary>
/// Options for [Module].
/// 
/// Configurable via appsettings.json:
/// <code>
/// {
///   "SharedCommon:[ModuleName]": {
///     "Property1": "value"
///   }
/// }
/// </code>
/// </summary>
public class [Options]
{
    /// <summary>
    /// [Description of property].
    /// Default: [default]
    /// </summary>
    [Required]
    public string Property1 { get; set; } = string.Empty;
    
    /// <summary>
    /// [Description of property].
    /// Default: [default]
    /// Range: [min]-[max]
    /// </summary>
    [Range(1, 10000)]
    public int Property2 { get; set; } = 300;
    
    /// <summary>
    /// [Description of property].
    /// Default: [default]
    /// Valid values: [values]
    /// </summary>
    public string Property3 { get; set; } = "DefaultValue";
}
```

---

## Error Handling Pattern

```csharp
try
{
    // External call
    var result = await externalService.GetAsync(id, ct);
    
    _logger.LogInformation(
        "Operation succeeded for {Id}",
        id);
    
    return new Result.Success(result);
}
catch (TimeoutException ex)
{
    _logger.LogWarning(ex,
        "External service timeout for {Id}",
        id);
    
    return new Result.Failure(
        "EXTERNAL_TIMEOUT",
        "Service timeout",
        ex);
}
catch (Exception ex)
{
    _logger.LogError(ex,
        "Unexpected error for {Id}",
        id);
    
    return new Result.Failure(
        "OPERATION_FAILED",
        "Operation failed",
        ex);
}
```

---

## Logging Pattern

```csharp
// Structured logging with context
using (LogContext.Property("OrderId", orderId))
using (LogContext.Property("CustomerId", customerId))
{
    _logger.LogInformation(
        "Processing order {OrderId}",
        orderId);
    
    try
    {
        // Do work
        _logger.LogInformation(
            "Order processed successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Failed to process order: {ErrorMessage}",
            ex.Message);
        
        throw;
    }
}
```

---

## Configuration Validation Pattern

```csharp
services.AddOptions<[Options]>()
    .BindConfiguration("SharedCommon:[ModuleName]")
    .ValidateDataAnnotations()
    .Validate(opts => 
    {
        if (opts.Required == null)
            return false;
        
        return true;
    }, "Property is required")
    .ValidateOnStart();
```

---

## Unit Test Template

```csharp
namespace SharedCommon.[ModuleName].UnitTests;

public class [ServiceName]Tests
{
    private readonly Mock<ILogger<[ServiceName]>> _loggerMock;
    private readonly Mock<IDependency> _dependencyMock;
    private readonly [ServiceName] _service;
    
    public [ServiceName]Tests()
    {
        _loggerMock = new();
        _dependencyMock = new();
        _service = new(
            _loggerMock.Object,
            _dependencyMock.Object);
    }
    
    [Fact]
    public async Task MethodAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var input = "valid-input";
        _dependencyMock
            .Setup(x => x.GetAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync("result");
        
        // Act
        var result = await _service.MethodAsync(input, CancellationToken.None);
        
        // Assert
        Assert.IsType<Result.Success>(result);
        _dependencyMock.Verify(x => 
            x.GetAsync(input, It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task MethodAsync_WithInvalidInput_ReturnsFailure()
    {
        // Arrange
        var input = "";
        
        // Act
        var result = await _service.MethodAsync(input, CancellationToken.None);
        
        // Assert
        Assert.IsType<Result.Failure>(result);
    }
}
```

---

## Integration Test Template

```csharp
namespace SharedCommon.[ModuleName].IntegrationTests;

[Collection(nameof(DockerCollection))]
public class [ServiceName]IntegrationTests : IAsyncLifetime
{
    private readonly DockerComposer _docker = new();
    private readonly [ServiceName] _service;
    
    public [ServiceName]IntegrationTests()
    {
        _service = new([dependencies from docker]);
    }
    
    public async Task InitializeAsync()
    {
        await _docker.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _docker.StopAsync();
    }
    
    [Fact]
    public async Task MethodAsync_WithRealDependencies_Succeeds()
    {
        // Arrange
        var input = "test-input";
        
        // Act
        var result = await _service.MethodAsync(input, CancellationToken.None);
        
        // Assert
        Assert.IsType<Result.Success>(result);
    }
}
```

---

## Architecture Test Template

```csharp
namespace SharedCommon.ArchitectureTests;

public class [ModuleName]ArchitectureTests
{
    [Fact]
    public void Module_DoesNotHaveCircularDependencies()
    {
        var result = Types
            .InAssembly(typeof([ModuleName]).Assembly)
            .Should()
            .NotHaveAnyCircularDependencies()
            .GetResult();
        
        Assert.True(result.IsSuccessful);
    }
    
    [Fact]
    public void AllPublicMethods_HaveXmlDocumentation()
    {
        var publicTypes = Types
            .InAssembly(typeof([ModuleName]).Assembly)
            .That()
            .ArePublic()
            .GetTypes();
        
        foreach (var type in publicTypes)
        {
            var hasDoc = !string.IsNullOrWhiteSpace(
                type.GetXmlDocsSummary());
            
            Assert.True(hasDoc, 
                $"Type {type.FullName} missing XML docs");
        }
    }
    
    [Fact]
    public void AllAsyncMethods_AcceptCancellationToken()
    {
        var asyncMethods = Types
            .InAssembly(typeof([ModuleName]).Assembly)
            .That()
            .ArePublic()
            .GetMethods()
            .Where(m => m.ReturnType.Name.Contains("Task"))
            .ToList();
        
        var missingCts = asyncMethods
            .Where(m => !m.GetParameters()
                .Any(p => p.ParameterType.Name == "CancellationToken"))
            .ToList();
        
        Assert.Empty(missingCts);
    }
}
```

---

## README.md Template

```markdown
# SharedCommon.[ModuleName]

[Concise description]

## Installation

```bash
dotnet add package SharedCommon.[ModuleName]
```

## Quick Start

### 1. Register

```csharp
builder.Services.Add[ModuleName](builder.Configuration);
```

### 2. Configure

```json
{
  "SharedCommon": {
    "[ModuleName]": {
      "Property1": "value"
    }
  }
}
```

### 3. Use

```csharp
public class MyService
{
    private readonly I[ServiceName] _service;
    
    public MyService(I[ServiceName] service)
    {
        _service = service;
    }
    
    public async Task DoWorkAsync(CancellationToken ct)
    {
        var result = await _service.MethodAsync("input", ct);
        
        return result switch
        {
            Result.Success success => "Done",
            Result.Failure failure => $"Error: {failure.Message}",
            _ => "Unknown"
        };
    }
}
```

## Configuration

See: [Link to full BRD configuration section]

## Error Handling

Errors are returned as `Result<T>`:

```csharp
var result = await service.MethodAsync(input, ct);

if (result is Result.Failure failure)
{
    // Handle error
    _logger.LogError("Error: {Message}", failure.Message);
}
```

## Extension Points

[Document how to extend/override]

## Troubleshooting

[Common issues and solutions]
```

---

## CLAUDE.md for Module

```markdown
# SharedCommon.[ModuleName]

[One-sentence description]

## API Surface

[Link to public interfaces]

## Configuration

[Link to configuration schema]

## Rules

**Must:**
- [Rule 1]
- [Rule 2]

**Forbidden:**
- [Anti-pattern 1]
- [Anti-pattern 2]

## Design Decisions

See: [Link to relevant ADRs]

## Testing Strategy

- Unit tests: [What's mocked]
- Integration tests: [What's real]

## Extension Points

- [How to extend]
```

---

## Dependency Injection Pattern

```csharp
// Always inject via constructor
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly I[Dependency] _dependency;
    private readonly IOptions<[Options]> _options;
    
    public MyService(
        ILogger<MyService> logger,
        I[Dependency] dependency,
        IOptions<[Options]> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
}
```

---

## Async/Await Pattern

```csharp
// ALWAYS include CancellationToken
public async Task<Result> MyMethodAsync(CancellationToken ct = default)
{
    try
    {
        // ALWAYS use ConfigureAwait(false) in libraries
        var result = await _dependency.GetAsync(ct)
            .ConfigureAwait(false);
        
        return new Result.Success(result);
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Operation cancelled");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed");
        throw;
    }
}
```

---

## Result Pattern

```csharp
// Always return Result<T> for expected failures
var result = operation();

return result switch
{
    // Success case
    var success when success.IsValid
        => new Result.Success(data),
    
    // Validation failure
    var invalid when !invalid.IsValid
        => new Result.Validation(errors),
    
    // Business logic failure
    var notfound when notfound == null
        => new Result.Failure("NOT_FOUND", "Item not found"),
    
    // Should never happen
    _ => throw new InvalidOperationException("Unexpected state")
};
```

---

## When to Throw Exceptions

**Throw when:**
- Configuration is invalid/missing at startup
- Precondition violated (null parameter, invalid state)
- Unrecoverable system error

**Don't throw when:**
- User input is invalid (return Result.Validation)
- Business logic fails (return Result.Failure)
- External service unavailable (return Result.Failure, degrade gracefully)

---

## Naming Conventions

| What | Pattern | Example |
|------|---------|---------|
| Interfaces | `I[What]` | `ILogger`, `ICacheService` |
| Classes | `[What]` | `Logger`, `CacheService` |
| Options | `[Feature]Options` | `LoggingOptions`, `CachingOptions` |
| Extensions | `ServiceCollectionExtensions` | `AddSharedCommonLogging()` |
| Tests | `[Class]Tests` | `LoggerTests` |
| Constants | `CONSTANT_CASE` | `DEFAULT_TTL` |
| Properties | `PascalCase` | `ApplicationName` |
| Parameters | `camelCase` | `applicationName` |
| Locals | `camelCase` | `result` |

---

## Do's & Don'ts

### ✅ DO

- Use `using` statements
- Validate input at entry point
- Log with context
- Use structured properties
- Handle cancellation tokens
- Inject dependencies
- Write XML docs
- Test error paths
- Use dependency injection
- Return Result<T> for expected failures

### ❌ DON'T

- Hardcode values
- Use `Console.WriteLine`
- Swallow exceptions
- Skip null checks
- Make methods > 30 lines
- Use static mutable state
- Use `var` for unclear types
- Mix async and sync
- Return null (use Result or Optional)
- Create circular dependencies

---

**Print this page. Keep it visible while implementing.**
