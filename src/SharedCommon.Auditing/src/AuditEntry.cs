namespace SharedCommon.Auditing;

/// <summary>
/// A single immutable audit log entry recording who did what to which entity and when.
/// </summary>
public sealed record AuditEntry
{
    /// <summary>Unique identifier for this audit entry.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp of the audited action.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Type of action performed.</summary>
    public AuditAction Action { get; init; }

    /// <summary>Name of the entity or aggregate type (e.g., "Order", "Customer").</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>String representation of the entity's primary key.</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>Authenticated user who performed the action. Null for system or anonymous actions.</summary>
    public string? UserId { get; init; }

    /// <summary>Tenant context for multi-tenant deployments. Null for single-tenant.</summary>
    public string? TenantId { get; init; }

    /// <summary>Correlation ID linking this audit entry to its originating HTTP request.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>JSON snapshot of the entity state before the action. Null for Create actions.</summary>
    public string? OldValues { get; init; }

    /// <summary>JSON snapshot of the entity state after the action. Null for Delete actions.</summary>
    public string? NewValues { get; init; }

    /// <summary>Names of properties that changed. Empty for Create/Delete.</summary>
    public IReadOnlyList<string> ChangedProperties { get; init; } = [];

    /// <summary>Arbitrary key/value metadata (e.g., IP address, user agent, feature flag context).</summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Type of audited action on an entity.</summary>
public enum AuditAction
{
    /// <summary>Entity was created.</summary>
    Created,

    /// <summary>Entity was updated (one or more properties changed).</summary>
    Updated,

    /// <summary>Entity was deleted (hard delete).</summary>
    Deleted,

    /// <summary>Entity was soft-deleted (IsDeleted flag set).</summary>
    SoftDeleted,

    /// <summary>Entity was read (recorded when sensitive data access must be logged).</summary>
    Accessed
}
