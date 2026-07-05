# ADR-006: Async/Await Patterns

**Status:** Accepted
**Date:** 2026-01-01

## Context

Inconsistent async patterns cause deadlocks, thread pool starvation, and hidden performance issues in library code. We need clear, enforced rules.

## Decision

### All I/O Must Be Async

- Every method that performs I/O (database, network, file) must be `async Task` or `async Task<T>`
- No synchronous wrappers around async code (`.Result`, `.Wait()`, `Task.Run(...).Result`)

### CancellationToken

Every public async method must accept `CancellationToken ct = default` as its last parameter:

```csharp
public Task<Order> GetOrderAsync(Guid id, CancellationToken ct = default);
```

### ConfigureAwait(false)

All library code (SharedCommon packages) must use `ConfigureAwait(false)` to avoid deadlocks when consumed by synchronization-context-heavy frameworks:

```csharp
var result = await _cache.GetAsync(key, ct).ConfigureAwait(false);
```

Application and API code (samples, consuming services) does not need `ConfigureAwait(false)`.

### ValueTask

Use `ValueTask<T>` for methods that are frequently synchronous (e.g., cache hits):

```csharp
public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);
```

Use `Task<T>` when the method is always async.

### Forbidden

- `async void` (except event handlers)
- `.Result` or `.Wait()` on tasks outside of `Main`/entry points
- Fire-and-forget without exception handling
- `Task.Run` to wrap CPU-bound work in I/O-async methods

## Consequences

- Library code safe for use in both WinForms/WPF and ASP.NET contexts
- No thread pool starvation under load
- CancellationToken enables graceful shutdown
