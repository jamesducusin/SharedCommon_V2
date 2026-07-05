using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Auditing;

/// <summary>Configuration for the SharedCommon auditing infrastructure.</summary>
public sealed class AuditOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "SharedCommon:Auditing";

    /// <summary>
    /// Storage backend for audit entries.
    /// Defaults to <see cref="AuditStorageBackend.Logging"/> (structured log output — no persistence).
    /// </summary>
    public AuditStorageBackend Backend { get; init; } = AuditStorageBackend.Logging;

    /// <summary>
    /// Whether to capture entity state snapshots (OldValues/NewValues).
    /// Disable if audit entries are high volume and snapshots are not required by compliance policy.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool CaptureValueSnapshots { get; init; } = true;

    /// <summary>
    /// Whether to record <see cref="AuditAction.Accessed"/> (read) events.
    /// Off by default — only enable when compliance mandates data access logging.
    /// </summary>
    public bool RecordReadAccess { get; init; } = false;

    /// <summary>
    /// Entity types to exclude from auditing.
    /// Useful for high-frequency, low-risk entities (e.g., session events, analytics).
    /// </summary>
    public IReadOnlyList<string> ExcludedEntityTypes { get; init; } = [];

    /// <summary>
    /// How long (in days) to retain audit entries.
    /// 0 = retain forever. Used by background cleanup jobs.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int RetentionDays { get; init; } = 0;
}

/// <summary>Available storage backends for audit log persistence.</summary>
public enum AuditStorageBackend
{
    /// <summary>
    /// Write audit entries to structured logs (default).
    /// No database table required. Cannot be queried. Suitable for log-aggregation pipelines.
    /// </summary>
    Logging,

    /// <summary>
    /// Persist audit entries to a relational database table via EF Core.
    /// Requires configuring <c>AuditDbContext</c> and running migrations.
    /// </summary>
    Database,

    /// <summary>
    /// Publish audit entries as domain events via <c>IMessagePublisher</c>.
    /// Requires SharedCommon.Messaging. Entries are consumed and stored by a dedicated audit service.
    /// </summary>
    Messaging
}
