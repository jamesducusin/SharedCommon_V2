namespace Templates.Domain.Common;

/// <summary>
/// Base interface for entities with identity.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public interface IEntity<TId> where TId : notnull
{
    /// <summary>
    /// Gets the entity's unique identifier.
    /// </summary>
    TId Id { get; }
}
