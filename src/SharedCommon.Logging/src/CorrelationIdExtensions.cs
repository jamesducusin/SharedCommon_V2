using Microsoft.AspNetCore.Http;
using SharedCommon.Core;

namespace SharedCommon.Logging;

/// <summary>
/// Extension methods for reading and writing the correlation ID on <see cref="HttpContext"/>.
/// Used by middleware to ensure every request carries a correlation ID before it reaches handlers.
/// </summary>
public static class CorrelationIdExtensions
{
    private const string ItemKey = "SharedCommon.CorrelationId";

    /// <summary>
    /// Returns the correlation ID stored on this request, or generates and stores a new one.
    /// The generated ID is also set as a response header using the default header name.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="headerName">Header to read from / write to. Default: <c>X-Correlation-ID</c>.</param>
    /// <returns>The resolved or newly created <see cref="CorrelationId"/>.</returns>
    public static CorrelationId GetOrCreateCorrelationId(
        this HttpContext context,
        string headerName = "X-Correlation-ID")
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(ItemKey, out var cached) && cached is CorrelationId existing)
            return existing;

        CorrelationId id;
        if (context.Request.Headers.TryGetValue(headerName, out var headerValue)
            && CorrelationId.TryCreate(headerValue, out var fromHeader))
        {
            id = fromHeader!;
        }
        else
        {
            id = CorrelationId.New();
        }

        context.Items[ItemKey] = id;
        context.Response.Headers[headerName] = id.Value;
        return id;
    }

    /// <summary>
    /// Stores a specific correlation ID on the request context and writes it to the response header.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="id">The correlation ID to assign.</param>
    /// <param name="headerName">Response header name. Default: <c>X-Correlation-ID</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="id"/> is null.</exception>
    public static void SetCorrelationId(
        this HttpContext context,
        CorrelationId id,
        string headerName = "X-Correlation-ID")
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);

        context.Items[ItemKey] = id;
        context.Response.Headers[headerName] = id.Value;
    }
}
