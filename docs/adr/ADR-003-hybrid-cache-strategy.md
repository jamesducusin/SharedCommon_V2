# ADR-003: Hybrid Cache Strategy

**Status:** Accepted
**Date:** 2026-01-01

## Context

Services need fast caching with high availability. Pure in-memory cache doesn't work across multiple service instances. Pure Redis adds network latency for every cache hit. We need a strategy that optimizes for the common case (cache hit) while supporting distributed invalidation.

## Decision

Use a **hybrid cache** (L1 in-memory + L2 Redis) via .NET 9's `HybridCache` (backported to .NET 8 via `Microsoft.Extensions.Caching.Hybrid`).

### Strategy

- L1 (in-memory): serves the majority of reads, sub-millisecond latency
- L2 (Redis): authoritative source, shared across instances, supports invalidation
- Read path: L1 hit → return; L1 miss → L2 hit → populate L1 → return; L2 miss → load → populate both
- Write path: write to source → invalidate L2 → L1 expires naturally via TTL

### Configuration

```json
{
  "Caching": {
    "L1": { "MaxSizeMb": 100, "DefaultTtlSeconds": 60 },
    "L2": { "ConnectionString": "", "DefaultTtlSeconds": 300 }
  }
}
```

### Stampede Protection

`HybridCache` uses built-in coalescing — concurrent requests for the same key deduplicate to a single load operation.

## Consequences

- Cache hits are extremely fast (in-memory)
- Multi-instance consistency maintained via Redis
- Single `ICacheService` abstraction hides the complexity
- Redis outage degrades to in-memory only (resilient)
