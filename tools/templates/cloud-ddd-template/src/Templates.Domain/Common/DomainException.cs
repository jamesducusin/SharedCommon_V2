namespace Templates.Domain.Common;

/// <summary>
/// Base class for domain exceptions.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; protected set; }

    /// <summary>
    /// Gets the HTTP status code for this error.
    /// </summary>
    public int StatusCode { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    protected DomainException(string message, string code, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}
