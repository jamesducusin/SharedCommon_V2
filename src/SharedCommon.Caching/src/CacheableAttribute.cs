namespace SharedCommon.Caching;

/// <summary>
/// Marks a method for automatic result caching when used with a caching interceptor
/// (e.g. Castle.DynamicProxy or a source-generated decorator).
///
/// The cache key is derived from <see cref="KeyPrefix"/> and the method arguments
/// serialized as a hash. The entry lives for <see cref="DurationSeconds"/> seconds.
///
/// Example:
/// <code>
/// [Cacheable(KeyPrefix = "user", DurationSeconds = 600)]
/// public async Task&lt;User&gt; GetUserAsync(string id, CancellationToken ct)
///     => await _repo.GetAsync(id, ct);
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CacheableAttribute : Attribute
{
    /// <summary>
    /// Prefix prepended to the generated cache key.
    /// Use a stable, entity-scoped value such as <c>"user"</c> or <c>"product"</c>.
    /// Default: empty string (method name is used).
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Cache entry lifetime in seconds.
    /// Default: 300 (5 minutes).
    /// </summary>
    public int DurationSeconds { get; set; } = 300;
}
