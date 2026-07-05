namespace SharedCommon.Core;

/// <summary>
/// Marks a type as a domain entity with a strongly-typed identifier.
/// Entities are distinguished by identity, not by attribute values.
/// </summary>
/// <typeparam name="TId">The identity type (e.g., <see cref="Guid"/>, <see cref="int"/>).</typeparam>
public interface IEntity<TId> where TId : notnull
{
    /// <summary>The unique identifier for this entity.</summary>
    TId Id { get; }
}

/// <summary>
/// Marks a type as a DDD value object.
/// Value objects have no identity; equality is determined by their attribute values.
/// Implement as a <c>record</c> to get structural equality automatically.
/// </summary>
public interface IValueObject { }

/// <summary>
/// Marks an entity as an aggregate root.
/// Only aggregate roots may be retrieved from repositories or referenced by foreign key from other aggregates.
/// </summary>
/// <typeparam name="TId">The identity type.</typeparam>
public interface IAggregateRoot<TId> : IEntity<TId> where TId : notnull
{
    /// <summary>
    /// Domain events raised during this aggregate's lifetime.
    /// Cleared after being dispatched by the infrastructure layer.
    /// </summary>
    IReadOnlyList<object> DomainEvents { get; }
}

/// <summary>
/// Contract for application startup health checks.
/// Implement to verify a dependency before the application begins serving traffic.
///
/// Example:
/// <code>
/// public class DatabaseHealthCheck : IStartupHealthCheck
/// {
///     public string Name => "database";
///     public async Task&lt;bool&gt; CheckAsync(CancellationToken ct)
///         => await _db.CanConnectAsync(ct);
/// }
/// </code>
/// </summary>
public interface IStartupHealthCheck
{
    /// <summary>Human-readable name for this health check.</summary>
    string Name { get; }

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if healthy; <c>false</c> if unhealthy.</returns>
    Task<bool> CheckAsync(CancellationToken ct);
}

/// <summary>
/// Allows packages to register their FluentValidation validators into a shared registry.
/// Implemented by packages that expose validators, consumed by SharedCommon.Validation.
/// </summary>
public interface IValidationProvider
{
    /// <summary>
    /// Registers validators into the provided registry.
    /// </summary>
    /// <param name="registry">The validator registry to register into.</param>
    void RegisterValidators(IValidatorRegistry registry);
}

/// <summary>
/// Registry for FluentValidation validators.
/// Used by <see cref="IValidationProvider"/> implementations.
/// </summary>
public interface IValidatorRegistry
{
    /// <summary>
    /// Registers a validator for a specific model type.
    /// </summary>
    /// <typeparam name="TModel">The model type to validate.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    void Register<TModel, TValidator>() where TValidator : class;
}

/// <summary>
/// Allows packages to register their OpenTelemetry instrumentation into a shared registry.
/// Implemented by packages that emit traces or metrics, consumed by SharedCommon.Observability.
/// </summary>
public interface IObservabilityProvider
{
    /// <summary>
    /// Registers instrumentation into the provided registry.
    /// </summary>
    /// <param name="registry">The observability registry.</param>
    void RegisterInstrumentation(IObservabilityRegistry registry);
}

/// <summary>
/// Registry for OpenTelemetry ActivitySource and Meter registrations.
/// </summary>
public interface IObservabilityRegistry
{
    /// <summary>Registers an activity source name for tracing.</summary>
    /// <param name="sourceName">The ActivitySource name (typically the package name).</param>
    void AddActivitySource(string sourceName);

    /// <summary>Registers a meter name for metrics.</summary>
    /// <param name="meterName">The Meter name (typically the package name).</param>
    void AddMeter(string meterName);
}
