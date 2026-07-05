namespace Templates.Application.Common.Telemetry;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for instrumenting domain operations with distributed tracing and metrics.
/// Integrates with OpenTelemetry for observability across the entire request lifecycle.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Start a new span for a domain operation.
    /// </summary>
    /// <param name="operationName">Name of the operation (e.g., "CreateOrder")</param>
    /// <param name="operationType">Category of operation (e.g., "command", "query", "event")</param>
    /// <returns>Activity span to track the operation</returns>
    IOperationScope StartOperation(string operationName, string operationType);

    /// <summary>
    /// Record a custom metric value.
    /// </summary>
    void RecordMetric(string metricName, long value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Record an operation duration.
    /// </summary>
    void RecordDuration(string operationName, TimeSpan duration, Dictionary<string, object>? tags = null);
}

/// <summary>
/// Represents the scope of a traced operation.
/// Disposes the activity when the operation completes.
/// </summary>
public interface IOperationScope : IDisposable
{
    /// <summary>
    /// Set a tag on the activity for additional context.
    /// </summary>
    void SetTag(string key, object? value);

    /// <summary>
    /// Record an exception on the activity.
    /// </summary>
    void RecordException(Exception ex);

    /// <summary>
    /// Mark the operation as failed.
    /// </summary>
    void MarkFailed(string reason);

    /// <summary>
    /// Mark the operation as succeeded.
    /// </summary>
    void MarkSucceeded();
}

/// <summary>
/// Default implementation of ITelemetryService using ActivitySource for distributed tracing.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private static readonly ActivitySource _activitySource = new("Templates.Application");
    private static readonly System.Diagnostics.Metrics.Meter _meter = new("Templates.Application");
    
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
    }

    public IOperationScope StartOperation(string operationName, string operationType)
    {
        var activity = _activitySource.StartActivity(operationName);
        
        if (activity != null)
        {
            activity.SetTag("operation.type", operationType);
            activity.SetTag("operation.timestamp", DateTime.UtcNow.ToUniversalTime());
            _logger.LogDebug("Starting operation: {OperationName} ({OperationType})", operationName, operationType);
        }

        return new OperationScope(activity, operationName, _logger);
    }

    public void RecordMetric(string metricName, long value, Dictionary<string, object>? tags = null)
    {
        // Note: Full metrics implementation would use System.Diagnostics.Metrics counters/histograms
        // This is a placeholder for counter increment
        _logger.LogDebug("Metric recorded: {MetricName}={Value}", metricName, value);
    }

    public void RecordDuration(string operationName, TimeSpan duration, Dictionary<string, object>? tags = null)
    {
        _logger.LogDebug(
            "Operation duration recorded: {OperationName}={DurationMs}ms",
            operationName, duration.TotalMilliseconds);
    }
}

/// <summary>
/// Implementation of IOperationScope that manages activity lifecycle.
/// </summary>
internal class OperationScope : IOperationScope
{
    private readonly Activity? _activity;
    private readonly string _operationName;
    private readonly ILogger<TelemetryService> _logger;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    public OperationScope(Activity? activity, string operationName, ILogger<TelemetryService> logger)
    {
        _activity = activity;
        _operationName = operationName;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();
    }

    public void SetTag(string key, object? value)
    {
        _activity?.SetTag(key, value);
    }

    public void RecordException(Exception ex)
    {
        _activity?.RecordException(ex);
        _activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        _logger.LogError(ex, "Operation failed: {OperationName}", _operationName);
    }

    public void MarkFailed(string reason)
    {
        _activity?.SetStatus(ActivityStatusCode.Error, reason);
        _logger.LogWarning("Operation marked as failed: {OperationName} - {Reason}", _operationName, reason);
    }

    public void MarkSucceeded()
    {
        _activity?.SetStatus(ActivityStatusCode.Ok);
        _stopwatch.Stop();
        _logger.LogDebug(
            "Operation succeeded: {OperationName} ({DurationMs}ms)",
            _operationName, _stopwatch.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _stopwatch.Stop();

        if (_activity != null)
        {
            // Set tags for duration if not already set
            _activity.SetTag("operation.duration_ms", _stopwatch.ElapsedMilliseconds);
            _activity.Dispose();
        }

        _disposed = true;
    }
}
