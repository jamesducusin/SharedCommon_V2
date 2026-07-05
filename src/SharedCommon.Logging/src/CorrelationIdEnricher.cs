using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using SharedCommon.Core;

namespace SharedCommon.Logging;

/// <summary>
/// Serilog enricher that injects the current request's correlation ID into every log event.
/// Reads from <see cref="IRequestContext"/> when available; falls back to the
/// <c>X-Correlation-ID</c> HTTP header (or configured alternative), then generates a new ID.
///
/// Registered automatically by <see cref="ServiceCollectionExtensions.AddSharedCommonLogging"/>.
/// </summary>
public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _headerName;
    private readonly string _propertyName;

    /// <summary>
    /// Initializes the enricher.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    /// <param name="options">Correlation ID configuration.</param>
    public CorrelationIdEnricher(
        IHttpContextAccessor httpContextAccessor,
        CorrelationIdOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(options);

        _httpContextAccessor = httpContextAccessor;
        _headerName = options.HeaderName;
        _propertyName = options.LogPropertyName;
    }

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = ResolveCorrelationId();
        var property = propertyFactory.CreateProperty(_propertyName, correlationId);
        logEvent.AddOrUpdateProperty(property);
    }

    private string ResolveCorrelationId()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return CorrelationId.New().Value;

        // Prefer IRequestContext if registered and populated.
        if (ctx.RequestServices.GetService(typeof(IRequestContext)) is IRequestContext requestContext
            && !string.IsNullOrWhiteSpace(requestContext.CorrelationId.Value))
        {
            return requestContext.CorrelationId.Value;
        }

        // Fall back to the HTTP header.
        if (ctx.Request.Headers.TryGetValue(_headerName, out var headerValue)
            && CorrelationId.TryCreate(headerValue, out var fromHeader))
        {
            return fromHeader!.Value;
        }

        return CorrelationId.New().Value;
    }
}
