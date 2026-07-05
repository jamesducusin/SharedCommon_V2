namespace SharedCommon.Auditing;

/// <summary>
/// Storage abstraction for audit entries. Implement this in your application's
/// data access layer (e.g., an EF Core DbContext) and register it in DI to use
/// <see cref="AuditStorageBackend.Database"/>.
///
/// Example EF Core implementation:
/// <code>
/// public class AppDbContext : DbContext, IAuditStore
/// {
///     public DbSet&lt;AuditEntry&gt; AuditLog { get; set; }
///
///     public async Task SaveAsync(AuditEntry entry, CancellationToken ct)
///     {
///         AuditLog.Add(entry);
///         await SaveChangesAsync(ct);
///     }
///
///     public async Task SaveBatchAsync(IEnumerable&lt;AuditEntry&gt; entries, CancellationToken ct)
///     {
///         AuditLog.AddRange(entries);
///         await SaveChangesAsync(ct);
///     }
///
///     public async Task&lt;IReadOnlyList&lt;AuditEntry&gt;&gt; GetHistoryAsync(
///         string entityType, string entityId, CancellationToken ct)
///         => await AuditLog
///             .Where(e => e.EntityType == entityType &amp;&amp; e.EntityId == entityId)
///             .OrderByDescending(e => e.OccurredAt)
///             .ToListAsync(ct);
/// }
/// </code>
/// </summary>
public interface IAuditStore
{
    /// <summary>Persists a single audit entry.</summary>
    Task SaveAsync(AuditEntry entry, CancellationToken ct = default);

    /// <summary>Persists a batch of audit entries.</summary>
    Task SaveBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default);

    /// <summary>Returns audit history for an entity, newest-first.</summary>
    Task<IReadOnlyList<AuditEntry>> GetHistoryAsync(string entityType, string entityId, CancellationToken ct = default);
}
