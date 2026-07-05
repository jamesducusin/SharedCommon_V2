# Performance Guidelines

See ADR-006 for async patterns. See ADR-003 for caching strategy.

## Allocation Rules

### Hot Paths (>1000 req/s)

- Use `Span<T>` / `Memory<T>` instead of array slices
- Use `ArrayPool<T>.Shared` for temporary buffers
- Avoid LINQ on hot paths — use `for` loops
- Avoid closures that capture variables (allocate on heap)
- Use `struct` for small, short-lived data (avoid boxing)

### String Handling

```csharp
// Good — uses span, no allocation for common case
if (header.AsSpan().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))

// Bad — allocates substring
if (header.StartsWith("Bearer "))
    var token = header.Substring(7);
```

### StringBuilder

```csharp
// Good
var sb = new StringBuilder();
foreach (var item in items) sb.Append(item.Name).Append(", ");

// Bad
var result = "";
foreach (var item in items) result += item.Name + ", ";
```

## Async Rules

See ADR-006 for full async patterns.

Key rules:
- `ConfigureAwait(false)` on all awaits in library code
- `ValueTask<T>` for frequently-synchronous paths
- No `Task.Run` to fake async I/O

## Caching Rules

See ADR-003 and `.claude/skills/caching/SKILL.md`.

- Cache read-heavy data with TTL appropriate for freshness requirements
- Measure cache hit rate before and after — cache must actually help
- Never cache unbounded result sets (page the query first)

## Benchmarking

Use BenchmarkDotNet for any optimization claim:

```csharp
[MemoryDiagnoser]
public class CacheBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task<string?> WithoutCache() => await _service.GetWithoutCacheAsync(_key);

    [Benchmark]
    public async Task<string?> WithCache() => await _service.GetWithCacheAsync(_key);
}
```

Add benchmarks to `tests/SharedCommon.PerformanceTests/BenchmarkTests.cs`.

## Database Patterns

- Project only needed columns — never `SELECT *`
- Paginate large result sets — never load unbounded collections
- Use `AsNoTracking()` for read-only queries (Entity Framework)
- Batch inserts/updates — never loop with individual calls
- Index filter columns — check query plans for table scans
