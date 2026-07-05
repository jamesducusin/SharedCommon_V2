namespace SharedCommon.ResponseBuilder;

/// <summary>Standardized success response envelope for all API endpoints.</summary>
/// <typeparam name="T">The type of the response payload.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Always <c>true</c> for success responses.</summary>
    public bool Success { get; init; } = true;

    /// <summary>The response payload.</summary>
    public T? Data { get; init; }

    /// <summary>Correlation ID from the originating request, for distributed tracing.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Pagination metadata for list responses. <c>null</c> for single-item responses.</summary>
    public PaginationInfo? Pagination { get; init; }

    /// <summary>Creates a success response wrapping <paramref name="data"/>.</summary>
    public static ApiResponse<T> Ok(T data, string? correlationId = null) =>
        new() { Data = data, CorrelationId = correlationId };

    /// <summary>Creates a paged success response.</summary>
    public static ApiResponse<T> Paged(T data, PaginationInfo pagination, string? correlationId = null) =>
        new() { Data = data, Pagination = pagination, CorrelationId = correlationId };
}

/// <summary>Pagination metadata included in list responses.</summary>
/// <param name="Page">Current page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
public sealed record PaginationInfo(int Page, int PageSize, int TotalCount)
{
    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;
}
