using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedCommon.Core;

namespace SharedCommon.ResponseBuilder;

/// <summary>
/// Creates RFC 9457 compliant <see cref="ProblemDetails"/> objects.
/// Use fully qualified name <c>SharedCommon.ResponseBuilder.ProblemDetailsFactory</c>
/// to avoid ambiguity with <c>Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory</c>.
/// </summary>
public static class ProblemDetailsFactory
{
    private const string CorrelationIdExtension = "correlationId";

    /// <summary>Creates a 404 Not Found problem.</summary>
    public static ProblemDetails NotFound(string detail, string? correlationId = null) =>
        Create(StatusCodes.Status404NotFound, "Not Found", detail, correlationId);

    /// <summary>Creates a 401 Unauthorized problem.</summary>
    public static ProblemDetails Unauthorized(string detail = "Authentication required.", string? correlationId = null) =>
        Create(StatusCodes.Status401Unauthorized, "Unauthorized", detail, correlationId);

    /// <summary>Creates a 403 Forbidden problem.</summary>
    public static ProblemDetails Forbidden(string detail = "Access denied.", string? correlationId = null) =>
        Create(StatusCodes.Status403Forbidden, "Forbidden", detail, correlationId);

    /// <summary>Creates a 409 Conflict problem.</summary>
    public static ProblemDetails Conflict(string detail, string? correlationId = null) =>
        Create(StatusCodes.Status409Conflict, "Conflict", detail, correlationId);

    /// <summary>Creates a 429 Too Many Requests problem.</summary>
    public static ProblemDetails TooManyRequests(string detail = "Too many requests. Please retry later.", string? correlationId = null) =>
        Create(StatusCodes.Status429TooManyRequests, "Too Many Requests", detail, correlationId);

    /// <summary>Creates a 500 Internal Server Error problem. Detail is intentionally generic.</summary>
    public static ProblemDetails InternalError(string? correlationId = null) =>
        Create(StatusCodes.Status500InternalServerError, "Internal Server Error",
            "An unexpected error occurred.", correlationId);

    /// <summary>Creates a 422 Unprocessable Entity validation problem with field-level errors.</summary>
    public static ValidationProblemDetails Validation(
        IDictionary<string, string[]> errors,
        string? correlationId = null)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred."
        };

        if (correlationId is not null)
            problem.Extensions[CorrelationIdExtension] = correlationId;

        return problem;
    }

    /// <summary>Creates a problem from a <see cref="Result.Failure"/> discriminated union case.</summary>
    public static ProblemDetails FromFailure(Result.Failure failure, string? correlationId = null) =>
        Create(StatusCodes.Status500InternalServerError, failure.Code, failure.Message, correlationId);

    /// <summary>Creates a problem from a <see cref="Result.Validation"/> discriminated union case.</summary>
    public static ValidationProblemDetails FromValidation(Result.Validation validation, string? correlationId = null) =>
        Validation(validation.Errors, correlationId);

    private static ProblemDetails Create(int status, string title, string detail, string? correlationId)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        if (correlationId is not null)
            problem.Extensions[CorrelationIdExtension] = correlationId;

        return problem;
    }
}
