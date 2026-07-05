# SharedCommon.Auditing

Structured audit trail for compliance and operational observability. Records actor, entity, action, and value snapshots. Pluggable storage backends: structured logging (default), relational database, or messaging.

## API Surface

- `IAuditService` — `RecordAsync`, `RecordBatchAsync`, `GetHistoryAsync`
- `AuditEntry` — immutable record: Action, EntityType, EntityId, UserId, TenantId, CorrelationId, OldValues, NewValues, ChangedProperties, Metadata
- `AuditAction` — `Created`, `Updated`, `Deleted`, `SoftDeleted`, `Accessed`
- `AuditBuilder` — fluent builder; `FromContext(IRequestContext)` copies actor fields in one call
- `IAuditStore` — implement in your EF Core DbContext to enable the Database backend
- `AuditOptions` — `Backend`, `CaptureValueSnapshots`, `RecordReadAccess`, `RetentionDays`, `ExcludedEntityTypes`
- `AddSharedAuditing(IConfiguration)` — DI registration

## Rules

**Must:**
- Always call `RecordAsync` after the state change is committed (not before)
- Use `AuditBuilder.FromContext(requestContext)` to propagate UserId, TenantId, CorrelationId
- Use `ExcludedEntityTypes` for high-volume low-risk entities rather than omitting audit calls entirely
- Register `IAuditStore` before `AddSharedAuditing` when using the Database backend

**Forbidden:**
- Recording audit entries inside transactions that may roll back (entry would outlive the change)
- Catching and swallowing errors from `IAuditService` (it catches internally; callers should not double-catch)
- Putting sensitive field values in `OldValues`/`NewValues` without data masking

## Design Decisions

`LoggingAuditService` is the zero-infrastructure default — works with any log aggregation pipeline.
`DatabaseAuditService` errors are caught and logged, never surfaced to callers, so auditing never breaks the main request flow.
`IAuditStore` is a narrow interface intentionally; callers never depend on EF Core directly.

## Test Strategy

- Unit test `AuditBuilder` fluent API — each builder method, `FromContext` propagation, `Build()` output
- Unit test `AuditEntry` record equality and immutability
- Integration tests use `DatabaseAuditService` with an in-memory SQLite store

## Extension Points

- Implement `IAuditStore` for relational persistence (EF Core, Dapper, etc.)
- Swap backends via `AuditOptions:Backend` without code changes
