namespace SharedCommon.Core;

/// <summary>
/// Ambient context for request-scoped data shared across all components in the call chain.
/// Registered as <c>Scoped</c> — one instance per HTTP request.
/// Populated by <c>CorrelationIdMiddleware</c> in SharedCommon.Middlewares.
///
/// Example:
/// <code>
/// public class MyService(IRequestContext context, ILogger&lt;MyService&gt; logger)
/// {
///     public Task DoWorkAsync(CancellationToken ct)
///     {
///         logger.LogInformation("Request {CorrelationId} by {UserId}",
///             context.CorrelationId, context.UserId);
///     }
/// }
/// </code>
/// </summary>
public interface IRequestContext
{
    /// <summary>Unique identifier for this request. Always set. Never null.</summary>
    CorrelationId CorrelationId { get; }

    /// <summary>Tenant identifier for multi-tenant scenarios. <c>null</c> for single-tenant.</summary>
    string? TenantId { get; }

    /// <summary>Authenticated user identifier. <c>null</c> for anonymous requests.</summary>
    string? UserId { get; }

    /// <summary>Arbitrary extensible properties for the current request scope.</summary>
    IDictionary<string, object> Properties { get; }
}
