namespace Templates.Domain.Common.Exceptions;

/// <summary>
/// Base exception for all domain-level errors.
/// Represents violations of business rules.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Error code for API response mapping.
    /// </summary>
    public abstract string ErrorCode { get; }

    /// <summary>
    /// HTTP status code for this error.
    /// </summary>
    public abstract int StatusCode { get; }

    /// <summary>
    /// Additional error details for client response.
    /// </summary>
    public virtual Dictionary<string, object>? Details { get; protected set; }

    /// <summary>
    /// Initializes a new instance of DomainException.
    /// </summary>
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of DomainException with inner exception.
    /// </summary>
    protected DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
