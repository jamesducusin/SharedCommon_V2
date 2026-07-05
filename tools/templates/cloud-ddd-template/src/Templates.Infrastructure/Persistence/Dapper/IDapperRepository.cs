using System.Data;
using Templates.Domain.Common;

namespace Templates.Infrastructure.Persistence.Dapper;

/// <summary>
/// Generic Dapper repository interface for high-performance database operations
/// using stored procedures. Provides strongly-typed access while maintaining
/// the flexibility and performance of Dapper for high-traffic scenarios.
/// </summary>
public interface IDapperRepository<TEntity, TId> where TEntity : IEntity
{
    /// <summary>
    /// Execute a stored procedure and map results to a single entity.
    /// </summary>
    Task<TEntity?> QuerySingleAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a stored procedure and return multiple results as enumerable.
    /// </summary>
    Task<IEnumerable<TEntity>> QueryAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a stored procedure and return a scalar value (count, id, etc).
    /// </summary>
    Task<T> QueryScalarAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a stored procedure for insert/update/delete operations.
    /// Returns number of affected rows.
    /// </summary>
    Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entity by ID using sp_GetById stored procedure.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with pagination using sp_List stored procedure.
    /// </summary>
    Task<IEnumerable<TEntity>> ListAsync(
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total count of entities using sp_Count stored procedure.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a transaction-based batch operation for multiple related operations.
    /// </summary>
    Task<T> ExecuteTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
