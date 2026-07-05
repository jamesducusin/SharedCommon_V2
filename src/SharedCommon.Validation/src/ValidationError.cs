namespace SharedCommon.Validation;

/// <summary>
/// Represents a single validation failure for a specific property.
/// Returned in the body of 400 validation error responses.
/// </summary>
/// <param name="Property">Name of the property that failed validation (camelCase).</param>
/// <param name="Code">Machine-readable error code (e.g. <c>NotEmpty</c>, <c>MaxLength</c>).</param>
/// <param name="Message">Human-readable description suitable for display to end users.</param>
/// <param name="AttemptedValue">The value that was rejected. May be <c>null</c> when not safe to echo (e.g. passwords).</param>
public record ValidationError(
    string Property,
    string Code,
    string Message,
    object? AttemptedValue = null);
