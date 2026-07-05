namespace SharedCommon.Core;

/// <summary>
/// Default mutable implementation of <see cref="IRequestContext"/>.
/// Registered as Scoped. Populated by <c>CorrelationIdMiddleware</c> on each request.
/// </summary>
public sealed class RequestContext : IRequestContext
{
    /// <inheritdoc />
    public CorrelationId CorrelationId { get; set; } = CorrelationId.New();

    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public string? UserId { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
