namespace Templates.Domain.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found.
/// HTTP Status: 404 Not Found
/// </summary>
public class EntityNotFoundException : DomainException
{
    public override string ErrorCode => "ENTITY_NOT_FOUND";
    public override int StatusCode => 404;

    /// <summary>
    /// Initializes a new instance of EntityNotFoundException.
    /// </summary>
    /// <param name="entityType">Type of entity that was not found (e.g., "Order")</param>
    /// <param name="entityId">ID of the entity</param>
    public EntityNotFoundException(string entityType, object entityId)
        : base($"{entityType} with ID '{entityId}' was not found")
    {
        Details = new()
        {
            { "entityType", entityType },
            { "entityId", entityId }
        };
    }

    /// <summary>
    /// Initializes a new instance of EntityNotFoundException with custom message.
    /// </summary>
    public EntityNotFoundException(string message) : base(message)
    {
    }
}
