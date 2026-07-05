namespace Templates.Api.Common.Models;

/// <summary>
/// Standardized error response returned by the API.
/// Used for all error scenarios (4xx, 5xx, validation, business rules, etc.)
/// </summary>
public record ApiErrorResponse(
    /// <summary>
    /// Unique trace ID for correlating with server logs. Use this when reporting issues.
    /// </summary>
    string TraceId,
    
    /// <summary>
    /// HTTP status code (400, 404, 409, 500, etc.)
    /// </summary>
    int StatusCode,
    
    /// <summary>
    /// Error details with code, message, and optional context.
    /// </summary>
    ErrorDetail Error,
    
    /// <summary>
    /// ISO 8601 timestamp when the error occurred.
    /// </summary>
    DateTime Timestamp = default)
{
    public ApiErrorResponse() : this(
        TraceId: string.Empty,
        StatusCode: 500,
        Error: new(
            Code: "INTERNAL_ERROR",
            Message: "An internal error occurred",
            Details: null),
        Timestamp: DateTime.UtcNow)
    {
    }
}

/// <summary>
/// Error detail information.
/// </summary>
public record ErrorDetail(
    /// <summary>
    /// Machine-readable error code (e.g., "ORDER_NOT_FOUND", "VALIDATION_ERROR")
    /// </summary>
    string Code,
    
    /// <summary>
    /// Human-readable error message. Safe to display to clients.
    /// </summary>
    string Message,
    
    /// <summary>
    /// Optional additional context (entity IDs, field names, etc.)
    /// </summary>
    Dictionary<string, object>? Details = null);
