namespace SharedCommon.Core.Exceptions;

/// <summary>
/// Base class for domain exceptions that map to specific HTTP status codes.
/// These represent expected failures; use them with <c>ExceptionHandlingMiddleware</c>.
///
/// Hierarchy:
/// <list type="bullet">
///   <item><see cref="NotFoundException"/> → 404</item>
///   <item><see cref="UnauthorizedException"/> → 401</item>
///   <item><see cref="ForbiddenException"/> → 403</item>
///   <item><see cref="ConflictException"/> → 409</item>
///   <item><see cref="TooManyRequestsException"/> → 429</item>
/// </list>
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>HTTP status code this exception maps to.</summary>
    public abstract int StatusCode { get; }

    /// <summary>Machine-readable error code included in the response body.</summary>
    public string Code { get; }

    /// <summary>Initializes a new domain exception.</summary>
    protected DomainException(string code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}

/// <summary>The requested resource was not found. Returns HTTP 404.</summary>
public sealed class NotFoundException : DomainException
{
    /// <inheritdoc />
    public override int StatusCode => 404;

    /// <param name="message">Human-readable description of what was not found.</param>
    public NotFoundException(string message) : base("NOT_FOUND", message) { }

    /// <param name="code">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    public NotFoundException(string code, string message) : base(code, message) { }
}

/// <summary>The request lacks valid authentication. Returns HTTP 401.</summary>
public sealed class UnauthorizedException : DomainException
{
    /// <inheritdoc />
    public override int StatusCode => 401;

    /// <param name="message">Human-readable description.</param>
    public UnauthorizedException(string message = "Authentication required.")
        : base("UNAUTHORIZED", message) { }
}

/// <summary>The authenticated user lacks the required permission. Returns HTTP 403.</summary>
public sealed class ForbiddenException : DomainException
{
    /// <inheritdoc />
    public override int StatusCode => 403;

    /// <param name="message">Human-readable description.</param>
    public ForbiddenException(string message = "Access denied.")
        : base("FORBIDDEN", message) { }
}

/// <summary>A conflict with the current state of the resource. Returns HTTP 409.</summary>
public sealed class ConflictException : DomainException
{
    /// <inheritdoc />
    public override int StatusCode => 409;

    /// <param name="message">Human-readable description.</param>
    public ConflictException(string message) : base("CONFLICT", message) { }

    /// <param name="code">Machine-readable code.</param>
    /// <param name="message">Human-readable description.</param>
    public ConflictException(string code, string message) : base(code, message) { }
}

/// <summary>The caller has exceeded their rate limit. Returns HTTP 429.</summary>
public sealed class TooManyRequestsException : DomainException
{
    /// <inheritdoc />
    public override int StatusCode => 429;

    /// <param name="message">Human-readable description.</param>
    public TooManyRequestsException(string message = "Too many requests. Please retry later.")
        : base("RATE_LIMITED", message) { }
}
