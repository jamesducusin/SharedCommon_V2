# Prompt: Performance Audit

Use this prompt to identify and fix performance issues.

---

## Prompt

```
Perform a performance audit on [module or file list].

Use the performance-review skill (.claude/skills/performance-review/SKILL.md).

Check for:
- Unnecessary allocations on hot paths
- Blocking async calls (.Result, .Wait())
- Missing ConfigureAwait(false) in library code
- N+1 query patterns
- Cache opportunities
- Oversized LINQ chains

For each finding:
- Location (file:line)
- Issue description
- Performance impact estimate
- Specific fix with code example

Reference docs/standards/performance-guidelines.md.
Add BenchmarkDotNet tests for any optimized methods to tests/SharedCommon.PerformanceTests/.
```

---

## Expected Output

Performance report with fixes and benchmark additions.
