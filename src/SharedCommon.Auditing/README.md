# SharedCommon.Auditing

Structured audit trail for compliance and operational observability. Records who did what to which entity and when. Pluggable storage backends (structured logs, database, or messaging). Zero-coupling to EF Core unless you choose the database backend.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Auditing
```

## Registration

```csharp
builder.Services.AddSharedAuditing(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Auditing": {
      "Backend": "Logging",
      "CaptureValueSnapshots": true,
      "RecordReadAccess": false,
      "RetentionDays": 90,
      "ExcludedEntityTypes": ["SessionEvent", "MetricSnapshot"]
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `Backend` | `Logging` | `Logging`, `Database`, or `Messaging`. |
| `CaptureValueSnapshots` | `true` | Whether to capture OldValues/NewValues JSON snapshots. Disable for high-volume, low-risk entities. |
| `RecordReadAccess` | `false` | Enable only when compliance requires data-access logging. |
| `RetentionDays` | `0` (forever) | Used by background cleanup jobs. |
| `ExcludedEntityTypes` | `[]` | Entity type names to skip entirely. |

### Storage Backends

| Backend | Persistence | Query support | Setup required |
|---------|------------|---------------|----------------|
| `Logging` (default) | Log aggregation pipeline only | No | None |
| `Database` | Relational table | Yes | Implement `IAuditStore` + register it |
| `Messaging` | Published as domain events | Depends on consumer | SharedCommon.Messaging |

---

## Recording Audit Entries

Inject `IAuditService` and call `RecordAsync` after any state-changing operation.

```csharp
public class OrderService(IAuditService audit, IRequestContext ctx)
{
    public async Task DeleteAsync(Guid orderId, CancellationToken ct)
    {
        await _repo.DeleteAsync(orderId, ct);

        await audit.RecordAsync(new AuditEntry
        {
            Action        = AuditAction.Deleted,
            EntityType    = "Order",
            EntityId      = orderId.ToString(),
            UserId        = ctx.UserId,
            TenantId      = ctx.TenantId,
            CorrelationId = ctx.CorrelationId
        }, ct);
    }
}
```

### Using the Fluent Builder

`AuditBuilder` is the recommended way to construct entries:

```csharp
var entry = AuditBuilder
    .For("Order", order.Id.ToString())
    .Action(AuditAction.Updated)
    .FromContext(requestContext)        // Copies UserId, TenantId, CorrelationId
    .Changed("Status", "Total")
    .OldValues(JsonSerializer.Serialize(oldOrder))
    .NewValues(JsonSerializer.Serialize(newOrder))
    .WithMetadata("ip", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown")
    .Build();

await audit.RecordAsync(entry, ct);
```

### Audit Actions

| Action | When to use |
|--------|------------|
| `Created` | Entity created (hard insert) |
| `Updated` | One or more properties changed |
| `Deleted` | Hard delete |
| `SoftDeleted` | IsDeleted flag set to true |
| `Accessed` | Sensitive data read (only when `RecordReadAccess: true`) |

---

## Database Backend

When `Backend: Database`, implement `IAuditStore` in your EF Core DbContext:

```csharp
public class AppDbContext : DbContext, IAuditStore
{
    public DbSet<AuditEntry> AuditLog { get; set; }

    public async Task SaveAsync(AuditEntry entry, CancellationToken ct)
    {
        AuditLog.Add(entry);
        await SaveChangesAsync(ct);
    }

    public async Task SaveBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken ct)
    {
        AuditLog.AddRange(entries);
        await SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetHistoryAsync(
        string entityType, string entityId, CancellationToken ct)
        => await AuditLog
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
}
```

Register the store **before** `AddSharedAuditing`:

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IAuditStore, AppDbContext>();
builder.Services.AddSharedAuditing(builder.Configuration);
```

---

## Querying Audit History

Available only with the `Database` backend:

```csharp
public class AuditController(IAuditService audit) : ControllerBase
{
    [HttpGet("orders/{id}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        var history = await audit.GetHistoryAsync("Order", id.ToString(), ct);
        return Ok(history);
    }
}
```

---

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IAuditService` | Scoped | Implementation depends on `Backend`. |
| `AuditOptions` | Singleton (Options) | Validated at startup. |
