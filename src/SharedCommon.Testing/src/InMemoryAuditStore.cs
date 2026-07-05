using SharedCommon.Auditing;
using System.Collections.Concurrent;

namespace SharedCommon.Testing;

/// <summary>
/// Thread-safe in-memory <see cref="IAuditStore"/> for unit and integration tests.
/// Inspect <see cref="Entries"/> to assert that audit entries were recorded.
/// </summary>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentBag<AuditEntry> _entries = new();

    /// <summary>All audit entries recorded since this store was created (or last cleared).</summary>
    public IReadOnlyList<AuditEntry> Entries => _entries.ToList();

    /// <summary>Total number of recorded entries.</summary>
    public int Count => _entries.Count;

    /// <inheritdoc />
    public Task SaveAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default)
    {
        foreach (var entry in entries)
            _entries.Add(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AuditEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        IReadOnlyList<AuditEntry> history = _entries
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAt)
            .ToList();
        return Task.FromResult(history);
    }

    /// <summary>Removes all recorded entries. Call in test setup to reset state between tests.</summary>
    public void Clear() => _entries.Clear();
}
