# Performance Review Skill

Identify and fix allocations, async issues, and caching gaps.

## When to Use This Skill

Triggers:
- Allocations on hot paths
- Async/await pattern concerns
- Caching strategy decisions
- Database query performance
- Memory pressure or GC issues

Ask Claude explicitly: "Use performance-review skill"

## Input (What You Provide)

- Code segment or module to review
- Performance concern or observed symptom

## Output (What You Get)

- Optimization report with prioritized findings
- Benchmark recommendations
- Specific code fixes

## Checklist

**Allocations:**
- [ ] No unnecessary boxing on hot paths
- [ ] Span<T>/Memory<T> used for buffer slicing
- [ ] StringBuilder used for concatenation loops
- [ ] ArrayPool<T> used for temporary arrays
- [ ] Struct vs class decision appropriate

**Async/Await:**
- [ ] No `.Result` or `.Wait()` blocking
- [ ] `ConfigureAwait(false)` in library code
- [ ] `ValueTask` for frequently-synchronous paths
- [ ] No `async void` except event handlers
- [ ] CancellationToken propagated through call chain

**Caching:**
- [ ] Hot data cached appropriately
- [ ] Cache keys deterministic and scoped
- [ ] Eviction policy documented
- [ ] Cache stampede protection (SemaphoreSlim or similar)
- [ ] TTL appropriate for data freshness requirements

**Database:**
- [ ] N+1 queries eliminated
- [ ] Projections used (no SELECT *)
- [ ] Pagination on large result sets
- [ ] Indexes considered for filter columns
- [ ] Bulk operations used where possible

**I/O:**
- [ ] Streams not fully buffered before processing
- [ ] Parallel I/O where independent
- [ ] Connection pooling in use
- [ ] Retry policies with backoff configured

## Decision Tree

```
Is the issue allocation-based?
  Yes → Profile with dotMemory/PerfView, apply Span<T>/ArrayPool
  No  → Continue

Is the issue async-related?
  Yes → Check for blocking calls, missing ConfigureAwait(false)
  No  → Continue

Is the issue cache-related?
  Yes → Use caching/SKILL.md for strategy
  No  → Continue

Is the issue database-related?
  Yes → Check query plans, projections, indexes
  No  → Benchmark to identify root cause
```

## Common Mistakes

❌ Premature optimization without profiling
- Why: Wastes time on non-bottlenecks
- Fix: Measure first with BenchmarkDotNet

❌ `Task.Run` to fake async
- Why: Wastes thread pool threads, not truly async
- Fix: Use genuinely async I/O

❌ `IEnumerable` LINQ chains re-evaluated multiple times
- Why: Hidden O(n²) behavior
- Fix: Materialize with `.ToList()` at the right point

## References

See: docs/standards/performance-guidelines.md
See: tests/SharedCommon.PerformanceTests/BenchmarkTests.cs
See: docs/adr/ADR-003-hybrid-cache-strategy.md
