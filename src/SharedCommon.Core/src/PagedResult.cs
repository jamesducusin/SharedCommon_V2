namespace SharedCommon.Core;

/// <summary>
/// Pagination parameters for list queries.
/// </summary>
/// <param name="Page">1-based page number. Must be ≥ 1.</param>
/// <param name="PageSize">Number of items per page. Must be between 1 and <see cref="MaxPageSize"/>.</param>
public sealed record Pagination(int Page, int PageSize)
{
    /// <summary>Maximum allowed page size to prevent runaway queries.</summary>
    public const int MaxPageSize = 200;

    /// <summary>Default page size when not specified by the caller.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Zero-based offset for SQL/LINQ skip operations.</summary>
    public int Offset => (Page - 1) * PageSize;

    /// <summary>Default pagination: page 1, 20 items.</summary>
    public static Pagination Default => new(1, DefaultPageSize);

    /// <summary>
    /// Validates and returns a <see cref="Pagination"/> instance, clamping out-of-range values.
    /// </summary>
    /// <param name="page">Requested page (clamped to ≥ 1).</param>
    /// <param name="pageSize">Requested size (clamped to [1, <see cref="MaxPageSize"/>]).</param>
    public static Pagination Of(int page, int pageSize) =>
        new(Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize));
}

/// <summary>
/// A paginated list of items with total count and navigation metadata.
/// </summary>
/// <typeparam name="T">Type of items in this page.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
/// <param name="Pagination">The pagination parameters used to produce this page.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    Pagination Pagination)
{
    /// <summary>Total number of pages given the current page size.</summary>
    public int TotalPages =>
        Pagination.PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / Pagination.PageSize);

    /// <summary>Whether there is a page after this one.</summary>
    public bool HasNextPage => Pagination.Page < TotalPages;

    /// <summary>Whether there is a page before this one.</summary>
    public bool HasPreviousPage => Pagination.Page > 1;

    /// <summary>Creates an empty result for the given pagination.</summary>
    public static PagedResult<T> Empty(Pagination pagination) =>
        new([], 0, pagination);

    /// <summary>Creates a <see cref="PagedResult{T}"/> from a full in-memory list, applying pagination.</summary>
    /// <param name="source">The full ordered list.</param>
    /// <param name="pagination">Pagination parameters.</param>
    public static PagedResult<T> From(IReadOnlyList<T> source, Pagination pagination)
    {
        var items = source
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .ToList();
        return new PagedResult<T>(items, source.Count, pagination);
    }
}
