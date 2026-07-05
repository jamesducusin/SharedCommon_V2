# SharedCommon.Core

Foundation package. Shared abstractions, result types, base interfaces.
Zero external dependencies — this is the innermost ring.

## API Surface

- `Result<T>` and `Error` for explicit error handling
- `IEntity`, `IValueObject`, `IAggregateRoot` base interfaces
- `PagedResult<T>` and `Pagination` for list operations
- `DomainException` hierarchy
- Common guard clauses: `Guard.AgainstNull`, `Guard.AgainstEmpty`

## Rules

**Must:**
- Zero `<PackageReference>` to external packages (Microsoft.Extensions.* abstractions are allowed)
- Zero `<ProjectReference>` to other SharedCommon packages
- All public types XML documented
- All public types have unit tests
- Nullable reference types enabled

**Forbidden:**
- Any I/O operations
- Any DI registrations of concrete types (interfaces only)
- Infrastructure concerns of any kind
- External NuGet packages with transitive dependencies

## Design Decisions

- `Result<T>` uses discriminated union pattern (IsSuccess/IsFailure)
- `Error` is a value type with Code, Description, and Type
- `IEntity<TId>` is generic to support both Guid and int IDs

## Test Strategy

- Pure unit tests — no mocking needed (no dependencies to mock)
- Test all Result<T> combinations
- Test all guard clause edge cases

## Extension Points

None by design. Core is stable; changes require careful review.
New types in Core affect every package.
