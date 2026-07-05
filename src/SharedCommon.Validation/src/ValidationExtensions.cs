using FluentValidation.Results;
using SharedCommon.Core;

namespace SharedCommon.Validation;

/// <summary>
/// Extension methods for converting FluentValidation results into <see cref="Result"/> variants.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Converts a <see cref="ValidationResult"/> into a <see cref="Result"/>.
    /// Returns <see cref="Result.Success"/> when the validation passed,
    /// or <see cref="Result.Validation"/> containing the grouped errors when it failed.
    /// </summary>
    /// <param name="validationResult">The FluentValidation result to convert.</param>
    /// <returns>A <see cref="Result"/> representing the outcome.</returns>
    public static Result ToResult(this ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        if (validationResult.IsValid)
            return new Result.Success();

        var errors = validationResult.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return new Result.Validation(errors);
    }

    /// <summary>
    /// Converts a <see cref="ValidationResult"/> into a typed <see cref="Result{T}"/>.
    /// Returns <see cref="Result{T}.Success"/> with <paramref name="value"/> when validation passed,
    /// or <see cref="Result{T}.Validation"/> when it failed.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="validationResult">The FluentValidation result to convert.</param>
    /// <param name="value">The value to wrap on success.</param>
    public static Result<T> ToResult<T>(this ValidationResult validationResult, T value)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        if (validationResult.IsValid)
            return Result<T>.Ok(value);

        var errors = validationResult.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return Result<T>.Invalid(errors);
    }

    /// <summary>
    /// Converts a <see cref="ValidationResult"/> into a list of <see cref="ValidationError"/> records.
    /// Useful for structured API error responses.
    /// </summary>
    /// <param name="validationResult">The FluentValidation result to convert.</param>
    public static IReadOnlyList<ValidationError> ToValidationErrors(this ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        return validationResult.Errors
            .Select(f => new ValidationError(
                Property: f.PropertyName,
                Code: f.ErrorCode,
                Message: f.ErrorMessage,
                AttemptedValue: f.AttemptedValue))
            .ToList();
    }
}
