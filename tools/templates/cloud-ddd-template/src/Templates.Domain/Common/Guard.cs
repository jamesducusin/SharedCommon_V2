namespace Templates.Domain.Common;

/// <summary>
/// Guard clauses for input validation and invariant checking.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws if the argument is null.
    /// </summary>
    public static void Against<T>(T? argument, string argumentName) where T : class
    {
        if (argument == null)
            throw new ArgumentNullException(argumentName, $"{argumentName} cannot be null");
    }

    /// <summary>
    /// Throws if the argument is null.
    /// </summary>
    public static void AgainstNull<T>(T? argument, string argumentName) where T : class
    {
        if (argument == null)
            throw new ArgumentNullException(argumentName, $"{argumentName} cannot be null");
    }

    /// <summary>
    /// Throws if the string argument is null or empty.
    /// </summary>
    public static void AgainstNullOrEmpty(string? argument, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException($"{argumentName} cannot be null or empty", argumentName);
    }

    /// <summary>
    /// Throws if the GUID argument is empty.
    /// </summary>
    public static void AgainstEmptyGuid(Guid argument, string argumentName)
    {
        if (argument == Guid.Empty)
            throw new ArgumentException($"{argumentName} cannot be an empty GUID", argumentName);
    }

    /// <summary>
    /// Throws if the value is less than the minimum.
    /// </summary>
    public static void AgainstLessThan<T>(T argument, T minimum, string argumentName) where T : IComparable<T>
    {
        if (argument.CompareTo(minimum) < 0)
            throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} cannot be less than {minimum}");
    }

    /// <summary>
    /// Throws if the value is less than or equal to the minimum.
    /// </summary>
    public static void AgainstLessThanOrEqual<T>(T argument, T minimum, string argumentName) where T : IComparable<T>
    {
        if (argument.CompareTo(minimum) <= 0)
            throw new ArgumentOutOfRangeException(argumentName, $"{argumentName} must be greater than {minimum}");
    }

    /// <summary>
    /// Throws if the collection is empty.
    /// </summary>
    public static void AgainstEmpty<T>(IEnumerable<T> argument, string argumentName)
    {
        if (!argument.Any())
            throw new ArgumentException($"{argumentName} cannot be empty", argumentName);
    }
}
