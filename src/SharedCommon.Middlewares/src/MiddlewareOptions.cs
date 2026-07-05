namespace SharedCommon.Middlewares;

/// <summary>
/// Top-level configuration for SharedCommon pipeline middlewares.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "Middlewares": {
///       "ExceptionHandling": { "Enabled": true, "IncludeStackTrace": false },
///       "CorrelationId": { "Enabled": true, "HeaderName": "X-Correlation-ID" },
///       "RequestLogging": { "Enabled": true, "ExcludePaths": ["/health", "/metrics"] }
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class MiddlewareOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Middlewares</c>.</summary>
    public const string SectionName = "SharedCommon:Middlewares";

    /// <summary>Exception handling settings.</summary>
    public ExceptionHandlingOptions ExceptionHandling { get; set; } = new();

    /// <summary>Correlation ID propagation settings.</summary>
    public CorrelationIdMiddlewareOptions CorrelationId { get; set; } = new();

    /// <summary>Request and response logging settings.</summary>
    public RequestLoggingOptions RequestLogging { get; set; } = new();
}

/// <summary>Exception handling middleware settings.</summary>
public sealed class ExceptionHandlingOptions
{
    /// <summary>Enable the exception handling middleware. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Include the exception stack trace in error responses.
    /// Default: <c>false</c>. Should only be <c>true</c> in non-Production environments.
    /// </summary>
    public bool IncludeStackTrace { get; set; } = false;

    /// <summary>Log all unhandled exceptions. Default: <c>true</c>.</summary>
    public bool LogExceptions { get; set; } = true;
}

/// <summary>Correlation ID middleware settings.</summary>
public sealed class CorrelationIdMiddlewareOptions
{
    /// <summary>Enable correlation ID middleware. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>HTTP header name for reading and propagating the correlation ID. Default: <c>X-Correlation-ID</c>.</summary>
    public string HeaderName { get; set; } = "X-Correlation-ID";

    /// <summary>Generate a new correlation ID when the header is absent. Default: <c>true</c>.</summary>
    public bool GenerateIfMissing { get; set; } = true;
}

/// <summary>Request and response logging middleware settings.</summary>
public sealed class RequestLoggingOptions
{
    /// <summary>Enable request logging. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Log the request body. Disabled by default to avoid leaking sensitive data. Default: <c>false</c>.</summary>
    public bool LogRequestBody { get; set; } = false;

    /// <summary>Log the response body. Disabled by default. Default: <c>false</c>.</summary>
    public bool LogResponseBody { get; set; } = false;

    /// <summary>Paths excluded from logging (e.g. health probes and metrics endpoints).</summary>
    public string[] ExcludePaths { get; set; } = ["/health", "/metrics"];

    /// <summary>Maximum body size captured in the log, in bytes. Default: 1024 (1 KB).</summary>
    public int MaxBodySizeToLog { get; set; } = 1024;
}
