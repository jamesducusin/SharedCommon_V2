using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;

namespace SharedCommon.Security;

/// <summary>
/// ASP.NET Core middleware that writes OWASP-recommended HTTP security headers on every response.
/// Controlled per-header via <see cref="SecurityHeadersOptions"/>.
/// Register via <see cref="ApplicationBuilderExtensions.UseSharedCommonSecurityHeaders"/>.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;

    /// <summary>Initializes the middleware.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.SecurityHeaders.Enabled)
            ApplyHeaders(context.Response);

        await _next(context).ConfigureAwait(false);
    }

    private void ApplyHeaders(HttpResponse response)
    {
        var headers = _options.SecurityHeaders;

        if (headers.StrictTransportSecurity.Enabled)
        {
            var hsts = new StringBuilder($"max-age={headers.StrictTransportSecurity.MaxAge}");
            if (headers.StrictTransportSecurity.IncludeSubdomains) hsts.Append("; includeSubDomains");
            if (headers.StrictTransportSecurity.Preload) hsts.Append("; preload");
            response.Headers["Strict-Transport-Security"] = hsts.ToString();
        }

        if (headers.ContentSecurityPolicy.Enabled)
        {
            var csp = headers.ContentSecurityPolicy;
            var builder = new StringBuilder();
            builder.Append($"default-src {csp.DefaultSrc}; ");
            builder.Append($"script-src {csp.ScriptSrc}; ");
            builder.Append($"style-src {csp.StyleSrc}; ");
            builder.Append($"img-src {csp.ImgSrc}");
            if (!string.IsNullOrWhiteSpace(csp.ReportUri))
                builder.Append($"; report-uri {csp.ReportUri}");
            response.Headers["Content-Security-Policy"] = builder.ToString();
        }

        if (headers.XContentTypeOptions.Enabled && headers.XContentTypeOptions.NoSniff)
            response.Headers["X-Content-Type-Options"] = "nosniff";

        if (headers.XFrameOptions.Enabled)
            response.Headers["X-Frame-Options"] = headers.XFrameOptions.Policy.ToUpperInvariant() switch
            {
                "SAMEORIGIN" => "SAMEORIGIN",
                _ => "DENY"
            };

        if (headers.ReferrerPolicy.Enabled)
            response.Headers["Referrer-Policy"] = headers.ReferrerPolicy.Policy;

        if (headers.PermissionsPolicy.Enabled)
        {
            var pp = headers.PermissionsPolicy;
            response.Headers["Permissions-Policy"] =
                $"camera={pp.Camera}, microphone={pp.Microphone}, geolocation={pp.Geolocation}";
        }

        // Remove headers that leak server information.
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("Server");
    }
}
