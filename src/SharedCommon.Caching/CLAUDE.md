# SharedCommon.Caching

Hybrid caching abstraction (L1 in-memory + L2 Redis).
Single `ICacheService` interface hiding cache topology from consumers.

## API Surface

- `ICacheService` — get, set, remove, exists
- `IDistributedCacheService` — distributed-only operations
- `CacheOptions` — TTL, Redis connection, size limits
- `AddSharedCaching(IConfiguration)` extension method

## Rules

**Must:**
- Use stampede protection (SemaphoreSlim or HybridCache coalescing)
- Log cache hits/misses at Debug level
- Never cache unbounded collections (validate TTL and key design)
- Handle Redis unavailability gracefully (fall back to in-memory)
- All keys follow `{package}:{entity}:{id}` convention

**Forbidden:**
- Business logic in cache key generation
- Caching security tokens (use dedicated token stores)
- Ignoring Redis connection failures silently
- Cache keys that include PII

## Design Decisions

See: docs/adr/ADR-003-hybrid-cache-strategy.md

## Test Strategy

**Unit Tests (81 behavioral tests):**
- Use in-memory `MemoryCache` implementation (no Redis required)
- `CacheServiceTests` (21): Core operations, stampede protection deduplication, batch ops, expiration, cancellation
- `CacheOptionsTests` (13): All configuration tiers validation, property access, multi-tier setup
- `CacheKeyValidationTests` (19): Key pattern convention, case sensitivity, exact matching, consistency
- `CacheErrorHandlingTests` (28): Null-safety, idempotence, concurrent call deduplication, exception types

**Integration Tests (future):**
- Require running Redis (docker-compose)
- Test cache eviction, TTL expiry, L1→L2 promotion, Redis unavailability graceful degradation

## Extension Points

- Custom `ICacheSerializer` for non-JSON serialization
- Custom `ICacheKeyBuilder` for domain-specific key patterns
- Additional cache backends via `ICacheService` implementation
