namespace Templates.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state of a resource.
/// HTTP Status: 409 Conflict
/// </summary>
public class ConflictException : DomainException
{
    public override string ErrorCode => "CONFLICT";
    public override int StatusCode => 409;

    /// <summary>
    /// Initializes a new instance of ConflictException.
    /// </summary>
    /// <param name="message">Description of the conflict</param>
    public ConflictException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of ConflictException with specific conflict details.
    /// </summary>
    public ConflictException(string message, Dictionary<string, object> details)
        : base(message)
    {
        Details = details;
    }
}
