using SharedCommon.Core;

namespace SharedCommon.Auditing;

/// <summary>
/// Fluent builder for constructing <see cref="AuditEntry"/> instances.
///
/// <code>
/// var entry = AuditBuilder
///     .For("Order", order.Id.ToString())
///     .Action(AuditAction.Updated)
///     .By(context.UserId)
///     .WithCorrelation(context.CorrelationId)
///     .Changed("Status", "Total")
///     .Build();
///
/// await auditService.RecordAsync(entry, ct);
/// </code>
/// </summary>
public sealed class AuditBuilder
{
    private readonly string _entityType;
    private readonly string _entityId;
    private AuditAction _action;
    private string? _userId;
    private string? _tenantId;
    private string? _correlationId;
    private string? _oldValues;
    private string? _newValues;
    private readonly List<string> _changedProperties = [];
    private readonly Dictionary<string, string> _metadata = new(StringComparer.OrdinalIgnoreCase);

    private AuditBuilder(string entityType, string entityId)
    {
        _entityType = entityType;
        _entityId = entityId;
    }

    /// <summary>Starts building an audit entry for an entity.</summary>
    /// <param name="entityType">Entity type name (e.g., "Order").</param>
    /// <param name="entityId">Primary key as string.</param>
    public static AuditBuilder For(string entityType, string entityId)
        => new(entityType, entityId);

    /// <summary>Sets the action type.</summary>
    public AuditBuilder Action(AuditAction action) { _action = action; return this; }

    /// <summary>Sets the user ID of the actor.</summary>
    public AuditBuilder By(string? userId) { _userId = userId; return this; }

    /// <summary>Sets the tenant context.</summary>
    public AuditBuilder ForTenant(string? tenantId) { _tenantId = tenantId; return this; }

    /// <summary>Propagates the request correlation ID.</summary>
    public AuditBuilder WithCorrelation(CorrelationId correlationId)
    {
        _correlationId = correlationId.Value;
        return this;
    }

    /// <summary>Propagates the request correlation ID from a string.</summary>
    public AuditBuilder WithCorrelation(string? correlationId) { _correlationId = correlationId; return this; }

    /// <summary>Sets the JSON snapshot of the entity before the change.</summary>
    public AuditBuilder OldValues(string? json) { _oldValues = json; return this; }

    /// <summary>Sets the JSON snapshot of the entity after the change.</summary>
    public AuditBuilder NewValues(string? json) { _newValues = json; return this; }

    /// <summary>Records which property names changed.</summary>
    public AuditBuilder Changed(params string[] properties)
    {
        _changedProperties.AddRange(properties);
        return this;
    }

    /// <summary>Adds arbitrary metadata key/value.</summary>
    public AuditBuilder WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>Populates actor fields from an <see cref="IRequestContext"/>.</summary>
    public AuditBuilder FromContext(IRequestContext context)
    {
        _userId = context.UserId;
        _tenantId = context.TenantId;
        _correlationId = context.CorrelationId.Value;
        return this;
    }

    /// <summary>Builds the immutable <see cref="AuditEntry"/>.</summary>
    public AuditEntry Build() => new()
    {
        Action = _action,
        EntityType = _entityType,
        EntityId = _entityId,
        UserId = _userId,
        TenantId = _tenantId,
        CorrelationId = _correlationId,
        OldValues = _oldValues,
        NewValues = _newValues,
        ChangedProperties = _changedProperties.AsReadOnly(),
        Metadata = _metadata
    };
}
