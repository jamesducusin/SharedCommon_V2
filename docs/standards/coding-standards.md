# Coding Standards

## Language Features

- **Nullable reference types:** `<Nullable>enable</Nullable>` in every project
- **Implicit usings:** enabled
- **File-scoped namespaces:** required
- **Primary constructors:** allowed for simple injection, required if >3 params
- **Pattern matching:** preferred over type casting
- **Records:** for immutable data transfer objects
- **Target-typed new:** allowed where type is obvious

## Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `OrderService` |
| Interfaces | I + PascalCase | `IOrderService` |
| Methods | PascalCase | `GetOrderAsync` |
| Parameters | camelCase | `orderId` |
| Private fields | _camelCase | `_logger` |
| Constants | PascalCase | `MaxRetryCount` |
| Async methods | Suffix `Async` | `GetOrderAsync` |

## Method Length

- Target: ≤ 20 lines
- Maximum: 30 lines (requires comment justifying why)
- Anything longer: extract to named private methods

## Comments

Write no comments unless the WHY is non-obvious. Good comment:
```csharp
// Redis SCAN instead of KEYS to avoid blocking the server under high load
```

Bad comment:
```csharp
// Get the order by ID
var order = await _repo.GetAsync(id, ct);
```

## XML Documentation

Required on all `public` and `protected` members:

```csharp
/// <summary>
/// Retrieves an order by its unique identifier.
/// </summary>
/// <param name="id">The order identifier.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>The order, or null if not found.</returns>
public Task<Order?> GetAsync(Guid id, CancellationToken ct = default);
```

## File Organization

```
namespace SharedCommon.Caching;           // file-scoped

public interface ICacheService { ... }    // interface first
public sealed class CacheService { ... } // implementation second
```

One type per file. Filename matches type name.

## Forbidden Patterns

- `var` for non-obvious types (use explicit type)
- Magic numbers and magic strings (use named constants or enums)
- `dynamic` type
- `#region` blocks
- Nested ternaries beyond one level
- `goto`

## Build Settings (Directory.Build.props)

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsAsErrors />
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```
