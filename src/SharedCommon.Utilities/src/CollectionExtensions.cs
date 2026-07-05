namespace SharedCommon.Utilities;

/// <summary>Extension methods for <see cref="IEnumerable{T}"/> and collection types.</summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Splits <paramref name="source"/> into sequential batches of at most <paramref name="size"/> elements.
    /// </summary>
    public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count < size) continue;
            yield return batch;
            batch = new List<T>(size);
        }

        if (batch.Count > 0) yield return batch;
    }

    /// <summary>Returns <c>true</c> if <paramref name="source"/> is non-null and contains at least one element.</summary>
    public static bool SafeAny<T>(this IEnumerable<T>? source) => source?.Any() == true;

    /// <summary>Returns <c>true</c> if <paramref name="source"/> is non-null and contains at least one matching element.</summary>
    public static bool SafeAny<T>(this IEnumerable<T>? source, Func<T, bool> predicate) =>
        source?.Any(predicate) == true;

    /// <summary>Filters out <c>null</c> elements, returning only non-null values.</summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class =>
        source.Where(item => item is not null)!;

    /// <summary>Executes <paramref name="action"/> on each element. Prefer LINQ projections where possible.</summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);
        foreach (var item in source) action(item);
    }

    /// <summary>Returns an empty sequence when <paramref name="source"/> is <c>null</c>.</summary>
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source) =>
        source ?? [];

    /// <summary>Converts a single value to a one-element <see cref="IEnumerable{T}"/>.</summary>
    public static IEnumerable<T> Yield<T>(this T value) { yield return value; }

    /// <summary>
    /// Returns <c>true</c> if the collection is <c>null</c> or contains no elements.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => !source.SafeAny();
}
