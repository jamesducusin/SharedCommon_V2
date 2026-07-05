# SharedCommon.Utilities

Lightweight, dependency-free utility extensions.
String, date, collection, and type helpers.

## API Surface

- `StringExtensions` — slug generation, truncation, masking (for logging)
- `DateTimeExtensions` — UTC normalization, business day calculations
- `CollectionExtensions` — batch processing, safe enumeration
- `TypeExtensions` — reflection helpers, type name utilities
- `Guard` — argument validation (re-exported from Core if needed here)

## Rules

**Must:**
- Zero external package dependencies
- All methods purely functional (no side effects)
- All methods tested with edge cases (null, empty, boundary)
- Performance-sensitive methods benchmarked

**Forbidden:**
- I/O of any kind
- DI registrations (utilities are pure static or extension methods)
- State of any kind

## Design Decisions

Utilities are the only package besides Core that is truly dependency-free.
If a utility needs a dependency, it belongs in a different package.

## Test Strategy

- Pure unit tests
- Test null inputs, empty inputs, Unicode edge cases
- Benchmark any method used in hot paths

## Extension Points

None by design. Add new utility methods following the existing patterns.
