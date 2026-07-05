using GreenDonut;
using Microsoft.Extensions.Logging;

namespace SharedCommon.GraphQL;

/// <summary>
/// Base class for all DataLoader implementations. Enforces N+1 prevention — all
/// relationship data must be loaded through a DataLoader, never via direct repository
/// calls inside resolvers.
///
/// Example:
/// <code>
/// public class OrdersByCustomerDataLoader(
///     IOrderRepository repo,
///     IBatchScheduler scheduler,
///     DataLoaderOptions options)
///     : DataLoaderBase&lt;Guid, IReadOnlyList&lt;Order&gt;&gt;(scheduler, options)
/// {
///     protected override async Task&lt;IReadOnlyList&lt;Result&lt;IReadOnlyList&lt;Order&gt;&gt;&gt;&gt; LoadBatchAsync(
///         IReadOnlyList&lt;Guid&gt; keys,
///         DataLoaderFetchContext&lt;IReadOnlyList&lt;Order&gt;&gt; context,
///         CancellationToken ct)
///     {
///         var orders = await repo.GetByCustomerIdsAsync(keys, ct);
///         return keys.Select(id =>
///             Result&lt;IReadOnlyList&lt;Order&gt;&gt;.Resolve(orders.GetValueOrDefault(id) ?? []))
///             .ToArray();
///     }
/// }
/// </code>
/// </summary>
/// <typeparam name="TKey">The batch key type (e.g., Guid, int).</typeparam>
/// <typeparam name="TValue">The resolved value type.</typeparam>
public abstract class DataLoaderBase<TKey, TValue>(
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<TKey, TValue>(batchScheduler, options)
    where TKey : notnull
{
}
