using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Logging;

/// <summary>
/// Top-level configuration for SharedCommon structured logging.
///
/// Configure via appsettings.json:
/// <code>
/// {
///   "SharedCommon": {
///     "Logging": {
///       "ApplicationName": "OrderService",
///       "MinimumLevel": "Information",
///       "Console": { "Enabled": true },
///       "File": { "Enabled": true, "Path": "./logs/app-.txt" }
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Logging</c>.</summary>
    public const string SectionName = "SharedCommon:Logging";

    /// <summary>Human-readable application name included in every log entry. Required, max 50 chars.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ApplicationName is required.")]
    [MaxLength(50, ErrorMessage = "ApplicationName must be 50 characters or fewer.")]
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>Minimum log level written to any sink. Default: <c>Information</c>.</summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>Write logs asynchronously to avoid blocking the request thread. Default: <c>true</c>.</summary>
    public bool AsyncMode { get; set; } = true;

    /// <summary>Log message patterns to suppress. Useful for silencing health-check noise.</summary>
    public string[] ExcludePatterns { get; set; } = [];

    /// <summary>Serilog pipeline settings.</summary>
    public SerilogOptions Serilog { get; set; } = new();

    /// <summary>Console sink settings.</summary>
    public ConsoleSinkOptions Console { get; set; } = new();

    /// <summary>Rolling file sink settings.</summary>
    public FileSinkOptions File { get; set; } = new();

    /// <summary>Elasticsearch sink settings.</summary>
    public ElasticsearchSinkOptions Elasticsearch { get; set; } = new();

    /// <summary>Database (SQL) sink settings.</summary>
    public DatabaseSinkOptions Database { get; set; } = new();

    /// <summary>Correlation ID propagation settings.</summary>
    public CorrelationIdOptions CorrelationId { get; set; } = new();
}

/// <summary>Serilog pipeline-level settings.</summary>
public sealed class SerilogOptions
{
    /// <summary>Enable the Serilog pipeline. When <c>false</c> the console fallback still runs. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Output format for all sinks. <c>Json</c> | <c>CompactJson</c> | <c>Text</c>. Default: <c>Json</c>.</summary>
    public string Format { get; set; } = "Json";

    /// <summary>Destructuring policy settings.</summary>
    public DestructureOptions Destructure { get; set; } = new();
}

/// <summary>Controls how complex objects are destructured during logging.</summary>
public sealed class DestructureOptions
{
    /// <summary>Apply destructuring limits. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum length of any serialised string property. Default: 4096.</summary>
    public int MaxStringLength { get; set; } = 4096;

    /// <summary>Maximum object graph depth. Default: 10.</summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>Maximum collection items serialised. Default: 100.</summary>
    public int MaxCollectionCount { get; set; } = 100;
}

/// <summary>Console sink settings.</summary>
public sealed class ConsoleSinkOptions
{
    /// <summary>Enable the console sink. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Console theme. <c>Colored</c> | <c>Grayscale</c> | <c>None</c>. Default: <c>Colored</c>.</summary>
    public string Theme { get; set; } = "Colored";

    /// <summary>Include timestamp in console output. Default: <c>true</c>.</summary>
    public bool IncludeTimestamp { get; set; } = true;
}

/// <summary>Rolling file sink settings.</summary>
public sealed class FileSinkOptions
{
    /// <summary>Enable the file sink. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Log file path. Required when enabled. Default: <c>./logs/app-.txt</c>.</summary>
    public string Path { get; set; } = "./logs/app-.txt";

    /// <summary>Number of rolled files to retain. Default: 30.</summary>
    public int RetainedFileCountLimit { get; set; } = 30;

    /// <summary>Rolling interval. <c>Infinite</c> | <c>Year</c> | <c>Month</c> | <c>Day</c> | <c>Hour</c> | <c>Minute</c>. Default: <c>Day</c>.</summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>Maximum single file size in bytes before rolling. Default: 1 GB.</summary>
    public long FileSizeLimit { get; set; } = 1_073_741_824;
}

/// <summary>Elasticsearch sink settings.</summary>
public sealed class ElasticsearchSinkOptions
{
    /// <summary>Enable the Elasticsearch sink. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Elasticsearch base URL. Required when enabled.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Basic-auth username. Optional — prefer managed identity or API keys.</summary>
    public string? Username { get; set; }

    /// <summary>Basic-auth password. Use secrets, never hardcode. Optional.</summary>
    public string? Password { get; set; }

    /// <summary>Index name template. Default: <c>logs-{0:yyyy.MM.dd}</c>.</summary>
    public string IndexFormat { get; set; } = "logs-{0:yyyy.MM.dd}";

    /// <summary>Documents per batch flush. Default: 500.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Flush interval in milliseconds. Default: 2000.</summary>
    public int Period { get; set; } = 2000;
}

/// <summary>Database (SQL) sink settings.</summary>
public sealed class DatabaseSinkOptions
{
    /// <summary>Enable the database sink. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>ADO.NET connection string. Required when enabled. Use secrets, never hardcode.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Target table name. Default: <c>Logs</c>.</summary>
    public string TableName { get; set; } = "Logs";

    /// <summary>Rows per bulk INSERT. Default: 100.</summary>
    public int BulkInsertBatchSize { get; set; } = 100;

    /// <summary>Days to retain log rows before cleanup. Default: 90.</summary>
    public int RetentionDays { get; set; } = 90;
}

/// <summary>Correlation ID propagation settings.</summary>
public sealed class CorrelationIdOptions
{
    /// <summary>Propagate correlation IDs into log entries. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>HTTP header name read and written. Default: <c>X-Correlation-ID</c>.</summary>
    public string HeaderName { get; set; } = "X-Correlation-ID";

    /// <summary>Structured log property name. Default: <c>CorrelationId</c>.</summary>
    public string LogPropertyName { get; set; } = "CorrelationId";
}
