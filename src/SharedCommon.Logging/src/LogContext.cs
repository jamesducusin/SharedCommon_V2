using Serilog.Context;

namespace SharedCommon.Logging;

/// <summary>
/// Pushes structured properties onto the ambient Serilog <see cref="Serilog.Context.LogContext"/>.
/// All properties pushed here are included in log entries written within the returned scope.
///
/// Example:
/// <code>
/// using (LogContext.Property("OrderId", orderId))
/// using (LogContext.Property("CustomerId", customerId))
/// {
///     _logger.LogInformation("Processing order");
/// }
/// </code>
/// </summary>
public static class LogContext
{
    /// <summary>
    /// Pushes a single named property onto the log context.
    /// Dispose the returned handle to remove the property from scope.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value. Nulls are recorded as the literal string <c>"null"</c>.</param>
    /// <returns>A handle that removes the property when disposed.</returns>
    public static IDisposable Property(string name, object? value)
        => Serilog.Context.LogContext.PushProperty(name, value);

    /// <summary>
    /// Pushes multiple named properties onto the log context in a single call.
    /// Dispose the returned handle to remove all pushed properties from scope.
    /// </summary>
    /// <param name="values">Dictionary of property names and values.</param>
    /// <returns>A combined handle that removes all properties when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    public static IDisposable Properties(IDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var handles = values
            .Select(kv => Serilog.Context.LogContext.PushProperty(kv.Key, kv.Value))
            .ToList();

        return new CompositeDisposable(handles);
    }

    private sealed class CompositeDisposable(IEnumerable<IDisposable> handles) : IDisposable
    {
        private readonly IReadOnlyList<IDisposable> _handles = handles.ToList();
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            foreach (var h in _handles) h.Dispose();
        }
    }
}
