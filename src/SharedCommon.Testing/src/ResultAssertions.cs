using SharedCommon.Core;
using Xunit;

namespace SharedCommon.Testing;

/// <summary>
/// xUnit assertion extensions for <see cref="Result{T}"/> and <see cref="Result"/>.
/// </summary>
public static class ResultAssertions
{
    /// <summary>
    /// Asserts the result is a <see cref="Result{T}.Success"/> and returns the typed data.
    /// Fails the test with a descriptive message if the result is a failure or validation error.
    /// </summary>
    public static T ShouldSucceed<T>(this Result<T> result)
    {
        var success = Assert.IsType<Result<T>.Success>(result);
        return success.Data;
    }

    /// <summary>
    /// Asserts the result is a <see cref="Result{T}.Failure"/> and optionally checks the error code.
    /// </summary>
    /// <param name="result">The result to assert.</param>
    /// <param name="expectedCode">When provided, also asserts the failure code matches exactly.</param>
    public static Result<T>.Failure ShouldFail<T>(this Result<T> result, string? expectedCode = null)
    {
        var failure = Assert.IsType<Result<T>.Failure>(result);
        if (expectedCode is not null)
            Assert.Equal(expectedCode, failure.Code);
        return failure;
    }

    /// <summary>
    /// Asserts the result is a <see cref="Result{T}.Validation"/> failure and optionally checks
    /// that a specific field has a validation error.
    /// </summary>
    /// <param name="result">The result to assert.</param>
    /// <param name="expectedField">When provided, asserts the named field appears in the error dictionary.</param>
    public static Result<T>.Validation ShouldBeInvalid<T>(this Result<T> result, string? expectedField = null)
    {
        var validation = Assert.IsType<Result<T>.Validation>(result);
        if (expectedField is not null)
            Assert.True(
                validation.Errors.ContainsKey(expectedField),
                $"Expected validation error on field '{expectedField}'. Actual errors: [{string.Join(", ", validation.Errors.Keys)}]");
        return validation;
    }

    /// <summary>Asserts an untyped <see cref="Result"/> is a <see cref="Result.Success"/>.</summary>
    public static void ShouldSucceed(this Result result) =>
        Assert.IsType<Result.Success>(result);

    /// <summary>
    /// Asserts an untyped <see cref="Result"/> is a <see cref="Result.Failure"/> and optionally checks the code.
    /// </summary>
    public static Result.Failure ShouldFail(this Result result, string? expectedCode = null)
    {
        var failure = Assert.IsType<Result.Failure>(result);
        if (expectedCode is not null)
            Assert.Equal(expectedCode, failure.Code);
        return failure;
    }
}
