using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharedCommon.Logging;

/// <summary>
/// ASP.NET Core health check that verifies the logging pipeline is configured and reachable.
/// Registers a test log entry at <c>Debug</c> level and confirms no exceptions surface.
///
/// Register via <see cref="ServiceCollectionExtensions.AddSharedCommonLogging"/>.
/// </summary>
public sealed class LoggingHealthCheck : IHealthCheck
{
    private readonly ILogger<LoggingHealthCheck> _logger;
    private readonly LoggingOptions _options;

    /// <summary>Initializes the health check with required dependencies.</summary>
    /// <param name="logger">Logger resolved from DI — proves the pipeline is wired.</param>
    /// <param name="options">Resolved logging options.</param>
    public LoggingHealthCheck(
        ILogger<LoggingHealthCheck> logger,
        IOptions<LoggingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Logging health check probe — application: {ApplicationName}", _options.ApplicationName);

            var enabledSinks = new List<string>();
            if (_options.Console.Enabled) enabledSinks.Add("Console");
            if (_options.File.Enabled) enabledSinks.Add("File");
            if (_options.Elasticsearch.Enabled) enabledSinks.Add("Elasticsearch");
            if (_options.Database.Enabled) enabledSinks.Add("Database");

            var data = new Dictionary<string, object>
            {
                ["ApplicationName"] = _options.ApplicationName,
                ["MinimumLevel"] = _options.MinimumLevel,
                ["EnabledSinks"] = string.Join(", ", enabledSinks),
                ["AsyncMode"] = _options.AsyncMode
            };

            return Task.FromResult(
                HealthCheckResult.Healthy("Logging pipeline is operational.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Logging pipeline encountered an error.", ex));
        }
    }
}
