# SharedCommon.[PackageName]

[One-line description of what this package does.]

## API Surface

- `I[PackageName]Service` — [primary interface description]
- `[PackageName]Options` — configuration options
- `Add[PackageName](IConfiguration)` — DI registration

## Rules

**Must:**
- [Rule 1]
- [Rule 2]
- All public APIs XML documented
- ILogger<T> injected in all services
- CancellationToken on all async methods

**Forbidden:**
- Hardcoded configuration values
- Static mutable state
- Console.WriteLine

## Design Decisions

See: docs/adr/ (link to relevant ADR if applicable)

## Test Strategy

- Unit tests in `tests/[PackageName].Tests/`
- Integration tests require [infrastructure dependency if any]

## Extension Points

- [Extension point 1]
