using System.Runtime.CompilerServices;
using SharedCommon.Core.Exceptions;

namespace SharedCommon.Core;

/// <summary>
/// Guard clauses for validating preconditions. Throws <see cref="ArgumentException"/> variants
/// for programming errors and <see cref="DomainException"/> variants for domain rule violations.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is <c>null</c>.
    /// </summary>
    /// <typeparam name="T">Reference or nullable value type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">Automatically captured from the call site.</param>
    /// <returns><paramref name="value"/> when not null.</returns>
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if the nullable value type is <c>null</c>.
    /// </summary>
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct
    {
        if (!value.HasValue)
            throw new ArgumentNullException(paramName);
        return value.Value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is null, empty, or whitespace.
    /// </summary>
    /// <param name="value">String to check.</param>
    /// <param name="paramName">Automatically captured from the call site.</param>
    /// <returns><paramref name="value"/> when not null/empty/whitespace.</returns>
    public static string AgainstNullOrEmpty(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value must not be null or empty.", paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is null, empty, or whitespace-only.
    /// </summary>
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be null, empty, or whitespace.", paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the collection is null or contains no elements.
    /// </summary>
    public static IEnumerable<T> AgainstEmpty<T>(
        IEnumerable<T>? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null || !value.Any())
            throw new ArgumentException("Collection must not be null or empty.", paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than <paramref name="min"/>.
    /// </summary>
    public static T AgainstLessThan<T>(
        T value,
        T min,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be at least {min}.");
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than <paramref name="max"/>.
    /// </summary>
    public static T AgainstGreaterThan<T>(
        T value,
        T max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be at most {max}.");
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is outside [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    public static T AgainstOutOfRange<T>(
        T value,
        T min,
        T max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is <see cref="Guid.Empty"/>.
    /// </summary>
    public static Guid AgainstEmptyGuid(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Guid must not be empty.", paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    /// <param name="condition">The condition that must be <c>false</c>.</param>
    /// <param name="message">Error message.</param>
    /// <param name="paramName">Parameter name for the exception.</param>
    public static void AgainstInvalidState(
        bool condition,
        string message,
        string? paramName = null)
    {
        if (condition)
            throw new ArgumentException(message, paramName);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> exceeds <paramref name="maxLength"/> characters.
    /// </summary>
    public static string AgainstExceedingLength(
        string value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        AgainstNull(value, paramName);
        if (value.Length > maxLength)
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", paramName);
        return value;
    }
}
