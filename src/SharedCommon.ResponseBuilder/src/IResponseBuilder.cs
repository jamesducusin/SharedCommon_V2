using Microsoft.AspNetCore.Mvc;
using SharedCommon.Core;

namespace SharedCommon.ResponseBuilder;

/// <summary>
/// Fluent builder for constructing <see cref="ApiResponse{T}"/> and <see cref="IActionResult"/> instances,
/// automatically injecting the current request's correlation ID.
/// </summary>
public interface IResponseBuilder
{
    /// <summary>Returns a 200 OK result wrapping <paramref name="data"/>.</summary>
    ActionResult<ApiResponse<T>> Ok<T>(T data);

    /// <summary>Returns a 200 OK result with a paged payload.</summary>
    ActionResult<ApiResponse<T>> Paged<T>(T data, PaginationInfo pagination);

    /// <summary>Returns a 201 Created result wrapping <paramref name="data"/>.</summary>
    ActionResult<ApiResponse<T>> Created<T>(string routeName, object? routeValues, T data);

    /// <summary>Maps a <see cref="Result{T}"/> to the appropriate <see cref="IActionResult"/>.</summary>
    IActionResult FromResult<T>(Result<T> result);

    /// <summary>Maps a <see cref="Result"/> to the appropriate <see cref="IActionResult"/>.</summary>
    IActionResult FromResult(Result result);
}
