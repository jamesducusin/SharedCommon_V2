using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SharedCommon.Core;

namespace SharedCommon.Middlewares;

/// <summary>
/// Ensures every request carries a correlation ID throughout its lifecycle.
///
/// Behaviour:
/// <list type="bullet">
///   <item>Reads <c>X-Correlation-ID</c> (or configured header) from the incoming request.</item>
///   <item>Generates a new ID when the header is missing and <c>GenerateIfMissing = true</c>.</item>
///   <item>Writes the ID back on the response header.</item>
///   <item>Populates <see cref="IRequestContext.CorrelationId"/> when that service is registered.</item>
/// </list>
///
/// Register via <see cref="ApplicationBuilderExtensions.UseSharedCommonCorrelationId"/>.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MiddlewareOptions _options;

    /// <summary>Initializes the middleware.</summary>
    public CorrelationIdMiddleware(RequestDelegate next, IOptions<MiddlewareOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var headerName = _options.CorrelationId.HeaderName;

        CorrelationId id;
        if (context.Request.Headers.TryGetValue(headerName, out var headerValue)
            && CorrelationId.TryCreate(headerValue, out var fromHeader))
        {
            id = fromHeader!;
        }
        else if (_options.CorrelationId.GenerateIfMissing)
        {
            id = CorrelationId.New();
        }
        else
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Write back to response.
        context.Response.Headers[headerName] = id.Value;

        // Populate IRequestContext when registered.
        if (context.RequestServices.GetService(typeof(IRequestContext)) is RequestContext requestContext)
            requestContext.CorrelationId = id;

        await _next(context).ConfigureAwait(false);
    }
}
