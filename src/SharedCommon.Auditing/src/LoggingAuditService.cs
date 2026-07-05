using Microsoft.Extensions.Logging;

namespace SharedCommon.Auditing;

/// <summary>
/// Default <see cref="IAuditService"/> implementation that writes structured audit entries
/// to the configured logger. Zero persistence — works with any log aggregation pipeline
/// (Elasticsearch, Seq, Loki, etc.).
///
/// Switch to a database or messaging backend by changing <c>AuditOptions.Backend</c>.
/// </summary>
internal sealed class LoggingAuditService(ILogger<LoggingAuditService> logger) : IAuditService
{
    public Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        WriteEntry(entry);
        return Task.CompletedTask;
    }

    public Task RecordBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default)
    {
        foreach (var entry in entries)
            WriteEntry(entry);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        // Logging backend does not support querying. Consumers should switch to Database backend
        // if audit history retrieval is required.
        logger.LogWarning(
            "GetHistoryAsync called on logging audit backend — no persistence, returning empty. " +
            "Switch to AuditStorageBackend.Database for query support. EntityType={EntityType} EntityId={EntityId}",
            entityType, entityId);

        return Task.FromResult<IReadOnlyList<AuditEntry>>([]);
    }

    private void WriteEntry(AuditEntry entry)
    {
        logger.LogInformation(
            "AUDIT {Action} {EntityType}/{EntityId} by {UserId} " +
            "correlation={CorrelationId} tenant={TenantId} " +
            "changed=[{ChangedProperties}] auditId={AuditId}",
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.UserId ?? "system",
            entry.CorrelationId,
            entry.TenantId,
            string.Join(", ", entry.ChangedProperties),
            entry.Id);
    }
}
