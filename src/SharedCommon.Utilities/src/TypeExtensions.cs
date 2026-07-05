namespace SharedCommon.Utilities;

/// <summary>Extension methods for <see cref="Type"/> reflection helpers.</summary>
public static class TypeExtensions
{
    /// <summary>
    /// Returns a human-readable type name that expands generic type arguments.
    /// For example, <c>List&lt;int&gt;</c> instead of <c>List`1</c>.
    /// </summary>
    public static string GetFriendlyName(this Type type)
    {
        if (!type.IsGenericType) return type.Name;

        var backtickIndex = type.Name.IndexOf('`', StringComparison.Ordinal);
        var baseName = backtickIndex >= 0 ? type.Name[..backtickIndex] : type.Name;
        var args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyName));
        return $"{baseName}<{args}>";
    }

    /// <summary>Returns <c>true</c> if the type is <see cref="Nullable{T}"/>.</summary>
    public static bool IsNullable(this Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// Returns the underlying type if <paramref name="type"/> is <see cref="Nullable{T}"/>;
    /// otherwise returns <paramref name="type"/> itself.
    /// </summary>
    public static Type UnwrapNullable(this Type type) =>
        Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>Returns <c>true</c> if <paramref name="type"/> implements <typeparamref name="TInterface"/>.</summary>
    public static bool Implements<TInterface>(this Type type) =>
        typeof(TInterface).IsAssignableFrom(type);

    /// <summary>Returns <c>true</c> if <paramref name="type"/> is a concrete, non-abstract class.</summary>
    public static bool IsConcrete(this Type type) =>
        type.IsClass && !type.IsAbstract;

    /// <summary>Returns <c>true</c> if <paramref name="type"/> is a closed generic of <paramref name="openGeneric"/>.</summary>
    public static bool IsClosedGenericOf(this Type type, Type openGeneric) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric;
}
