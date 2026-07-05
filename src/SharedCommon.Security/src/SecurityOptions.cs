using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Security;

/// <summary>
/// Top-level configuration for SharedCommon security features.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "Security": {
///       "SecurityHeaders": { "Enabled": true },
///       "RateLimit": { "Enabled": true, "Policies": { "Default": { "MaxRequests": 100, "WindowSeconds": 60 } } }
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Security</c>.</summary>
    public const string SectionName = "SharedCommon:Security";

    /// <summary>HTTP security headers settings.</summary>
    public SecurityHeadersOptions SecurityHeaders { get; set; } = new();

    /// <summary>Rate limiting settings.</summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>Input validation settings.</summary>
    public InputValidationOptions InputValidation { get; set; } = new();

    /// <summary>CORS settings.</summary>
    public CorsOptions Cors { get; set; } = new();

    /// <summary>HTTPS enforcement settings.</summary>
    public HttpsOptions Https { get; set; } = new();
}

/// <summary>HTTP security headers configuration.</summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>Enable security headers middleware. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>HSTS settings.</summary>
    public HstsOptions StrictTransportSecurity { get; set; } = new();

    /// <summary>Content-Security-Policy settings.</summary>
    public CspOptions ContentSecurityPolicy { get; set; } = new();

    /// <summary>X-Content-Type-Options settings.</summary>
    public XContentTypeOptions XContentTypeOptions { get; set; } = new();

    /// <summary>X-Frame-Options settings.</summary>
    public XFrameOptions XFrameOptions { get; set; } = new();

    /// <summary>Referrer-Policy settings.</summary>
    public ReferrerPolicyOptions ReferrerPolicy { get; set; } = new();

    /// <summary>Permissions-Policy settings.</summary>
    public PermissionsPolicyOptions PermissionsPolicy { get; set; } = new();
}

/// <summary>HSTS (HTTP Strict Transport Security) settings.</summary>
public sealed class HstsOptions
{
    /// <summary>Emit Strict-Transport-Security header. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>max-age in seconds. Default: 31536000 (1 year).</summary>
    [Range(0, int.MaxValue)]
    public int MaxAge { get; set; } = 31_536_000;

    /// <summary>Include the <c>includeSubDomains</c> directive. Default: <c>true</c>.</summary>
    public bool IncludeSubdomains { get; set; } = true;

    /// <summary>Include the <c>preload</c> directive. Default: <c>false</c>.</summary>
    public bool Preload { get; set; } = false;
}

/// <summary>Content Security Policy settings.</summary>
public sealed class CspOptions
{
    /// <summary>Emit Content-Security-Policy header. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>default-src directive. Default: <c>'self'</c>.</summary>
    public string DefaultSrc { get; set; } = "'self'";

    /// <summary>script-src directive. Default: <c>'self'</c>.</summary>
    public string ScriptSrc { get; set; } = "'self'";

    /// <summary>style-src directive. Default: <c>'self'</c>.</summary>
    public string StyleSrc { get; set; } = "'self'";

    /// <summary>img-src directive. Default: <c>'self' data:</c>.</summary>
    public string ImgSrc { get; set; } = "'self' data:";

    /// <summary>report-uri directive. Optional.</summary>
    public string? ReportUri { get; set; }
}

/// <summary>X-Content-Type-Options settings.</summary>
public sealed class XContentTypeOptions
{
    /// <summary>Emit X-Content-Type-Options header. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Set header to <c>nosniff</c>. Default: <c>true</c>.</summary>
    public bool NoSniff { get; set; } = true;
}

/// <summary>X-Frame-Options settings.</summary>
public sealed class XFrameOptions
{
    /// <summary>Emit X-Frame-Options header. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Frame policy. <c>Deny</c> | <c>SameOrigin</c>. Default: <c>Deny</c>.</summary>
    public string Policy { get; set; } = "Deny";
}

/// <summary>Referrer-Policy settings.</summary>
public sealed class ReferrerPolicyOptions
{
    /// <summary>Emit Referrer-Policy header. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Referrer policy value. Default: <c>strict-origin</c>.</summary>
    public string Policy { get; set; } = "strict-origin";
}

/// <summary>Permissions-Policy settings.</summary>
public sealed class PermissionsPolicyOptions
{
    /// <summary>Emit Permissions-Policy header. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>camera feature value. Default: empty (disable).</summary>
    public string Camera { get; set; } = "()";

    /// <summary>microphone feature value. Default: empty (disable).</summary>
    public string Microphone { get; set; } = "()";

    /// <summary>geolocation feature value. Default: empty (disable).</summary>
    public string Geolocation { get; set; } = "()";
}

/// <summary>Rate limiting configuration.</summary>
public sealed class RateLimitOptions
{
    /// <summary>Enable rate limiting. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Backend for rate limit counters. <c>Memory</c> | <c>Redis</c>. Default: <c>Memory</c>.</summary>
    public string Backend { get; set; } = "Memory";

    /// <summary>Named rate limit policies. Keyed by policy name.</summary>
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new()
    {
        ["Default"] = new RateLimitPolicyOptions { MaxRequests = 100, WindowSeconds = 60 },
        ["Authenticated"] = new RateLimitPolicyOptions { MaxRequests = 1000, WindowSeconds = 60 },
        ["ApiEndpoint"] = new RateLimitPolicyOptions { MaxRequests = 10_000, WindowSeconds = 3600 }
    };

    /// <summary>Response header carrying the remaining request count. Default: <c>X-RateLimit-Remaining</c>.</summary>
    public string HeaderName { get; set; } = "X-RateLimit-Remaining";
}

/// <summary>A single named rate limit policy.</summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Maximum requests allowed within the window. Default: 100.</summary>
    [Range(1, int.MaxValue)]
    public int MaxRequests { get; set; } = 100;

    /// <summary>Sliding window duration in seconds. Default: 60.</summary>
    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>Input validation settings.</summary>
public sealed class InputValidationOptions
{
    /// <summary>Enable input validation middleware. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum URL length in characters. Default: 2048.</summary>
    [Range(1, int.MaxValue)]
    public int MaxUrlLength { get; set; } = 2048;

    /// <summary>Maximum query string length in characters. Default: 8192.</summary>
    [Range(1, int.MaxValue)]
    public int MaxQueryStringLength { get; set; } = 8192;

    /// <summary>Maximum request body size in bytes. Default: 10 MB.</summary>
    [Range(1, int.MaxValue)]
    public int MaxBodySizeBytes { get; set; } = 10_485_760;

    /// <summary>Reject requests that contain common attack patterns (SQL injection, XSS, path traversal). Default: <c>true</c>.</summary>
    public bool BlockSuspiciousPatterns { get; set; } = true;
}

/// <summary>CORS configuration.</summary>
public sealed class CorsOptions
{
    /// <summary>Enable CORS policy. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Allowed origins. Required when enabled.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Allowed HTTP methods. Default: <c>GET, POST, PUT, DELETE</c>.</summary>
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE"];

    /// <summary>Allowed HTTP headers. Default: all (<c>*</c>).</summary>
    public string[] AllowedHeaders { get; set; } = ["*"];

    /// <summary>Allow credentials (cookies, authorization headers). Default: <c>false</c>.</summary>
    public bool AllowCredentials { get; set; } = false;

    /// <summary>Preflight max-age in seconds. Default: 3600.</summary>
    [Range(0, int.MaxValue)]
    public int MaxAge { get; set; } = 3600;
}

/// <summary>HTTPS enforcement settings.</summary>
public sealed class HttpsOptions
{
    /// <summary>Redirect HTTP requests to HTTPS. Default: <c>true</c>.</summary>
    public bool Enforced { get; set; } = true;

    /// <summary>HTTP status code used for the redirect. Default: 307 (Temporary Redirect).</summary>
    public int RedirectStatusCode { get; set; } = 307;
}
