using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedCommon.Core;

namespace SharedCommon.ResponseBuilder;

/// <summary>Extension methods that map <see cref="Result{T}"/> to <see cref="ApiResponse{T}"/> and <see cref="IActionResult"/>.</summary>
public static class ResponseBuilderExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="ApiResponse{T}"/>.
    /// Returns <c>null</c> for non-success results — prefer <see cref="ToActionResult{T}"/> at API boundaries.
    /// </summary>
    public static ApiResponse<T>? ToApiResponse<T>(this Result<T> result, string? correlationId = null) =>
        result is Result<T>.Success s ? ApiResponse<T>.Ok(s.Data, correlationId) : null;

    /// <summary>
    /// Maps a <see cref="Result{T}"/> to the appropriate HTTP <see cref="IActionResult"/>:
    /// 200 on success, 422 on validation, or a mapped error status on failure.
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, string? correlationId = null) =>
        result switch
        {
            Result<T>.Success s => new OkObjectResult(ApiResponse<T>.Ok(s.Data, correlationId)),
            Result<T>.Validation v => new UnprocessableEntityObjectResult(
                ProblemDetailsFactory.FromValidation(new Result.Validation(v.Errors), correlationId)),
            Result<T>.Failure f => MapToErrorResult(new Result.Failure(f.Code, f.Message, f.Exception), correlationId),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps a <see cref="Result"/> to the appropriate HTTP <see cref="IActionResult"/>:
    /// 204 on success, 422 on validation, or a mapped error status on failure.
    /// </summary>
    public static IActionResult ToActionResult(this Result result, string? correlationId = null) =>
        result switch
        {
            Result.Success => new NoContentResult(),
            Result.Validation v => new UnprocessableEntityObjectResult(
                ProblemDetailsFactory.FromValidation(v, correlationId)),
            Result.Failure f => MapToErrorResult(f, correlationId),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    private static ObjectResult MapToErrorResult(Result.Failure failure, string? correlationId)
    {
        var (status, problem) = failure.Code switch
        {
            "NOT_FOUND" => (StatusCodes.Status404NotFound,
                ProblemDetailsFactory.NotFound(failure.Message, correlationId)),
            "UNAUTHORIZED" => (StatusCodes.Status401Unauthorized,
                ProblemDetailsFactory.Unauthorized(failure.Message, correlationId)),
            "FORBIDDEN" => (StatusCodes.Status403Forbidden,
                ProblemDetailsFactory.Forbidden(failure.Message, correlationId)),
            "CONFLICT" => (StatusCodes.Status409Conflict,
                ProblemDetailsFactory.Conflict(failure.Message, correlationId)),
            "RATE_LIMITED" => (StatusCodes.Status429TooManyRequests,
                ProblemDetailsFactory.TooManyRequests(failure.Message, correlationId)),
            _ => (StatusCodes.Status500InternalServerError,
                ProblemDetailsFactory.FromFailure(failure, correlationId))
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
