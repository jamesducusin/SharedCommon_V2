namespace SharedCommon.GraphQL;

/// <summary>
/// Relay-compliant cursor-based pagination edge.
/// </summary>
/// <typeparam name="T">Node type.</typeparam>
public sealed record Edge<T>(T Node, string Cursor);

/// <summary>
/// Relay-compliant cursor-based page info returned with every connection.
/// </summary>
public sealed record PageInfo(
    bool HasNextPage,
    bool HasPreviousPage,
    string? StartCursor,
    string? EndCursor);

/// <summary>
/// Relay-compliant connection type for cursor-based pagination.
///
/// Use this as the return type on all list GraphQL fields:
/// <code>
/// [UsePaging]
/// public Connection&lt;Order&gt; GetOrders([Service] IOrderService svc)
///     => Connection.From(svc.GetAll(), totalCount: svc.Count());
/// </code>
/// </summary>
/// <typeparam name="T">Node type exposed in the connection.</typeparam>
public sealed record Connection<T>(
    IReadOnlyList<Edge<T>> Edges,
    PageInfo PageInfo,
    int TotalCount)
{
    /// <summary>
    /// Builds a <see cref="Connection{T}"/> from a sequential list, assigning
    /// index-based base-64 cursors.
    /// </summary>
    public static Connection<T> From(
        IReadOnlyList<T> items,
        int totalCount,
        int offset = 0)
    {
        var edges = items
            .Select((item, i) => new Edge<T>(item, EncodeCursor(offset + i)))
            .ToList();

        return new Connection<T>(
            edges,
            new PageInfo(
                HasNextPage: offset + items.Count < totalCount,
                HasPreviousPage: offset > 0,
                StartCursor: edges.Count > 0 ? edges[0].Cursor : null,
                EndCursor: edges.Count > 0 ? edges[^1].Cursor : null),
            totalCount);
    }

    private static string EncodeCursor(int index)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"cursor:{index}"));
}
