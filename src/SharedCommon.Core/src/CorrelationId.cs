namespace SharedCommon.Core;

/// <summary>
/// Unique request identifier for tracing a single request across services and systems.
/// Immutable value type; validates content at creation.
///
/// Example:
/// <code>
/// var id = CorrelationId.New();           // New GUID-backed ID
/// var id = CorrelationId.From("abc-123"); // From existing header value
/// string value = id;                      // Implicit conversion to string
/// </code>
/// </summary>
public sealed record CorrelationId
{
    /// <summary>The underlying string value of this correlation ID.</summary>
    public string Value { get; }

    private CorrelationId(string value) => Value = value;

    /// <summary>Creates a new correlation ID backed by a randomly generated GUID.</summary>
    /// <returns>A new <see cref="CorrelationId"/>.</returns>
    public static CorrelationId New() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates a <see cref="CorrelationId"/> from an existing string value.
    /// </summary>
    /// <param name="value">The existing correlation ID string (e.g., from an HTTP header).</param>
    /// <returns>A <see cref="CorrelationId"/> wrapping the provided value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
    public static CorrelationId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Correlation ID cannot be null, empty, or whitespace.", nameof(value));

        return new(value);
    }

    /// <summary>Tries to create a <see cref="CorrelationId"/> from a string, returning null if invalid.</summary>
    /// <param name="value">Input value.</param>
    /// <param name="id">Result if successful.</param>
    /// <returns><c>true</c> if successful.</returns>
    public static bool TryCreate(string? value, out CorrelationId? id)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            id = null;
            return false;
        }

        id = new(value);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Implicit conversion to <see cref="string"/>.</summary>
    public static implicit operator string(CorrelationId id) => id.Value;
}
