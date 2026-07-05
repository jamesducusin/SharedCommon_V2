using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedCommon.Core;

namespace SharedCommon.ResponseBuilder;

/// <summary>
/// Default implementation of <see cref="IResponseBuilder"/>.
/// Registered as Scoped — one instance per HTTP request so the correlation ID is consistent.
/// </summary>
public sealed class ResponseBuilder(IRequestContext requestContext) : IResponseBuilder
{
    private string CorrelationId => requestContext.CorrelationId.Value;

    /// <inheritdoc />
    public ActionResult<ApiResponse<T>> Ok<T>(T data) =>
        new OkObjectResult(ApiResponse<T>.Ok(data, CorrelationId));

    /// <inheritdoc />
    public ActionResult<ApiResponse<T>> Paged<T>(T data, PaginationInfo pagination) =>
        new OkObjectResult(ApiResponse<T>.Paged(data, pagination, CorrelationId));

    /// <inheritdoc />
    public ActionResult<ApiResponse<T>> Created<T>(string routeName, object? routeValues, T data) =>
        new CreatedAtRouteResult(routeName, routeValues, ApiResponse<T>.Ok(data, CorrelationId));

    /// <inheritdoc />
    public IActionResult FromResult<T>(Result<T> result) => result switch
    {
        Result<T>.Success s => new OkObjectResult(ApiResponse<T>.Ok(s.Data, CorrelationId)),
        Result<T>.Validation v => new UnprocessableEntityObjectResult(
            ProblemDetailsFactory.FromValidation(new Result.Validation(v.Errors), CorrelationId)),
        Result<T>.Failure f => MapFailure(new Result.Failure(f.Code, f.Message, f.Exception)),
        _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
    };

    /// <inheritdoc />
    public IActionResult FromResult(Result result) => result switch
    {
        Result.Success => new OkResult(),
        Result.Validation v => new UnprocessableEntityObjectResult(
            ProblemDetailsFactory.FromValidation(v, CorrelationId)),
        Result.Failure f => MapFailure(f),
        _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
    };

    private ObjectResult MapFailure(Result.Failure failure)
    {
        var (status, problem) = failure.Code switch
        {
            "NOT_FOUND" => (StatusCodes.Status404NotFound,
                ProblemDetailsFactory.NotFound(failure.Message, CorrelationId)),
            "UNAUTHORIZED" => (StatusCodes.Status401Unauthorized,
                ProblemDetailsFactory.Unauthorized(failure.Message, CorrelationId)),
            "FORBIDDEN" => (StatusCodes.Status403Forbidden,
                ProblemDetailsFactory.Forbidden(failure.Message, CorrelationId)),
            "CONFLICT" => (StatusCodes.Status409Conflict,
                ProblemDetailsFactory.Conflict(failure.Message, CorrelationId)),
            "RATE_LIMITED" => (StatusCodes.Status429TooManyRequests,
                ProblemDetailsFactory.TooManyRequests(failure.Message, CorrelationId)),
            _ => (StatusCodes.Status500InternalServerError,
                ProblemDetailsFactory.FromFailure(failure, CorrelationId))
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
