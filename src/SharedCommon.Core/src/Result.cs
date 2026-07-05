namespace SharedCommon.Core;

/// <summary>
/// Result pattern for operation outcomes — no exceptions for expected failures.
///
/// Usage:
/// <code>
/// return result switch
/// {
///     Result.Success s    => Ok(s.Data),
///     Result.Failure f    => BadRequest(f.Message),
///     Result.Validation v => UnprocessableEntity(v.Errors),
///     _                   => StatusCode(500)
/// };
/// </code>
/// </summary>
public abstract record Result
{
    /// <summary>Successful operation. Optional untyped payload.</summary>
    public sealed record Success(object? Data = null) : Result;

    /// <summary>Operation failed. Machine-readable <see cref="Code"/>, human-readable <see cref="Message"/>.</summary>
    public sealed record Failure(string Code, string Message, Exception? Exception = null) : Result;

    /// <summary>Input validation failed. Per-field error arrays.</summary>
    public sealed record Validation(IDictionary<string, string[]> Errors) : Result;

    /// <summary>Returns <c>true</c> when this is a <see cref="Success"/> result.</summary>
    public virtual bool IsSuccess => this is Success;

    /// <summary>Returns <c>true</c> when this is a <see cref="Failure"/> or <see cref="Validation"/> result.</summary>
    public bool IsFailure => !IsSuccess;
}

/// <summary>
/// Typed result with a strongly-typed success payload.
/// </summary>
/// <typeparam name="T">Type of the success data.</typeparam>
public abstract record Result<T> : Result
{
    /// <summary>Successful operation with typed data.</summary>
    public new sealed record Success(T Data) : Result<T>;

    /// <summary>Operation failed.</summary>
    public new sealed record Failure(string Code, string Message, Exception? Exception = null) : Result<T>;

    /// <summary>Input validation failed.</summary>
    public new sealed record Validation(IDictionary<string, string[]> Errors) : Result<T>;

    /// <inheritdoc />
    public override bool IsSuccess => this is Success;

    /// <summary>Creates a <see cref="Success"/> result.</summary>
    /// <param name="data">The success payload.</param>
    public static Result<T> Ok(T data) => new Success(data);

    /// <summary>Creates a <see cref="Failure"/> result.</summary>
    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <param name="exception">Original exception, if any. Never propagated to clients.</param>
    public static Result<T> Fail(string code, string message, Exception? exception = null)
        => new Failure(code, message, exception);

    /// <summary>Creates a <see cref="Validation"/> failure from a field-error dictionary.</summary>
    /// <param name="errors">Dictionary of field name to error message array.</param>
    public static Result<T> Invalid(IDictionary<string, string[]> errors)
        => new Validation(errors);

    /// <summary>Creates a <see cref="Validation"/> failure for a single field.</summary>
    /// <param name="property">Field name.</param>
    /// <param name="error">Error message.</param>
    public static Result<T> Invalid(string property, string error)
        => new Validation(new Dictionary<string, string[]> { [property] = [error] });
}
