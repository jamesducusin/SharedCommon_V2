using SharedCommon.Core;

namespace SharedCommon.Testing;

/// <summary>
/// Configurable <see cref="IRequestContext"/> for unit tests.
/// Pre-seeded with a deterministic correlation ID so assertions are predictable.
/// </summary>
public sealed class TestRequestContext : IRequestContext
{
    /// <summary>Deterministic correlation ID used across all tests unless overridden.</summary>
    public static readonly CorrelationId DefaultCorrelationId = CorrelationId.From("test-correlation-00000000");

    /// <inheritdoc />
    public CorrelationId CorrelationId { get; set; } = DefaultCorrelationId;

    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public string? UserId { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> Properties { get; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a pre-configured context for a specific tenant and user.</summary>
    public static TestRequestContext For(string userId, string tenantId) =>
        new() { UserId = userId, TenantId = tenantId };
}
