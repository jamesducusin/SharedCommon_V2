namespace SharedCommon.Auditing;

/// <summary>
/// Records audit entries for compliance and operational observability.
/// Inject and call <see cref="RecordAsync"/> after any state-changing operation.
///
/// Example:
/// <code>
/// public class OrderService(IAuditService audit, IRequestContext ctx)
/// {
///     public async Task DeleteAsync(Guid orderId, CancellationToken ct)
///     {
///         await _repo.DeleteAsync(orderId, ct);
///
///         await audit.RecordAsync(new AuditEntry
///         {
///             Action       = AuditAction.Deleted,
///             EntityType   = "Order",
///             EntityId     = orderId.ToString(),
///             UserId       = ctx.UserId,
///             CorrelationId = ctx.CorrelationId
///         }, ct);
///     }
/// }
/// </code>
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Persists a single audit entry.
    /// Implementations must be fire-safe — a write failure must never surface to callers.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordAsync(AuditEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Persists a batch of audit entries atomically (all-or-nothing where the store supports it).
    /// </summary>
    /// <param name="entries">Entries to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default);

    /// <summary>
    /// Retrieves audit history for a specific entity.
    /// Returns entries newest-first.
    /// </summary>
    /// <param name="entityType">Entity type name (e.g., "Order").</param>
    /// <param name="entityId">Entity primary key string.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<AuditEntry>> GetHistoryAsync(string entityType, string entityId, CancellationToken ct = default);
}
