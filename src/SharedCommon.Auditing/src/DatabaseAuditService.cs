using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharedCommon.Auditing;

/// <summary>
/// <see cref="IAuditService"/> implementation that persists entries to a relational database.
/// Requires a concrete <see cref="IAuditStore"/> to be registered by the consuming application
/// (typically an EF Core DbContext implementing <see cref="IAuditStore"/>).
///
/// Register by setting <c>AuditOptions.Backend = Database</c> and providing
/// an <see cref="IAuditStore"/> implementation in DI.
/// </summary>
internal sealed class DatabaseAuditService(
    IAuditStore store,
    IOptions<AuditOptions> options,
    ILogger<DatabaseAuditService> logger) : IAuditService
{
    private readonly AuditOptions _options = options.Value;

    public async Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        if (IsExcluded(entry.EntityType))
            return;

        try
        {
            await store.SaveAsync(entry, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to persist audit entry {AuditId} for {Action} on {EntityType}/{EntityId}",
                entry.Id, entry.Action, entry.EntityType, entry.EntityId);
        }
    }

    public async Task RecordBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default)
    {
        var filtered = entries
            .Where(e => !IsExcluded(e.EntityType))
            .ToList();

        if (filtered.Count == 0)
            return;

        try
        {
            await store.SaveBatchAsync(filtered, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist audit batch of {Count} entries", filtered.Count);
        }
    }

    public Task<IReadOnlyList<AuditEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
        => store.GetHistoryAsync(entityType, entityId, ct);

    private bool IsExcluded(string entityType)
        => _options.ExcludedEntityTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase);
}
