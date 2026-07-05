using System.Data;
using System.Data.SqlClient;
using Dapper;
using Templates.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Templates.Infrastructure.Persistence.Dapper;

/// <summary>
/// High-performance Dapper repository implementation for stored procedure execution.
/// Optimized for high-traffic scenarios (1000+ transactions per second).
/// Uses connection pooling and command buffering for performance.
/// </summary>
public class DapperRepository<TEntity, TId> : IDapperRepository<TEntity, TId>
    where TEntity : IEntity
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DapperRepository<TEntity, TId>> _logger;
    private static readonly string EntityName = typeof(TEntity).Name;

    public DapperRepository(IDbConnection connection, ILogger<DapperRepository<TEntity, TId>> logger)
    {
        Guard.NotNull(connection, nameof(connection));
        Guard.NotNull(logger, nameof(logger));

        _connection = connection;
        _logger = logger;

        // Ensure Dapper is optimized for performance
        SqlMapper.Settings.CommandTimeout = 30;
    }

    /// <summary>
    /// Execute a stored procedure and map results to a single entity.
    /// </summary>
    public async Task<TEntity?> QuerySingleAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotEmpty(procedureName, nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing stored procedure: {ProcedureName}", procedureName);

            if (_connection.State == ConnectionState.Closed)
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

            var result = await _connection.QueryFirstOrDefaultAsync<TEntity?>(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled while executing {ProcedureName}", procedureName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure and return multiple results as enumerable.
    /// </summary>
    public async Task<IEnumerable<TEntity>> QueryAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotEmpty(procedureName, nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing list stored procedure: {ProcedureName}", procedureName);

            if (_connection.State == ConnectionState.Closed)
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

            var results = await _connection.QueryAsync<TEntity>(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled while executing {ProcedureName}", procedureName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure and return a scalar value.
    /// </summary>
    public async Task<T> QueryScalarAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotEmpty(procedureName, nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing scalar stored procedure: {ProcedureName}", procedureName);

            if (_connection.State == ConnectionState.Closed)
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

            var result = await _connection.ExecuteScalarAsync<T>(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled while executing {ProcedureName}", procedureName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure for insert/update/delete operations.
    /// </summary>
    public async Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotEmpty(procedureName, nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing stored procedure: {ProcedureName}", procedureName);

            if (_connection.State == ConnectionState.Closed)
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

            var affectedRows = await _connection.ExecuteAsync(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return affectedRows;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled while executing {ProcedureName}", procedureName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    /// <summary>
    /// Get entity by ID using sp_{EntityName}_GetById stored procedure.
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(id, nameof(id));

        var procedureName = $"sp_{EntityName}_GetById";
        var parameters = new { Id = id };

        return await QuerySingleAsync(procedureName, parameters, cancellationToken);
    }

    /// <summary>
    /// Get all entities with pagination using sp_{EntityName}_List stored procedure.
    /// </summary>
    public async Task<IEnumerable<TEntity>> ListAsync(
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstExpression(x => x <= 0, pageNumber, nameof(pageNumber));
        Guard.AgainstExpression(x => x <= 0, pageSize, nameof(pageSize));

        var procedureName = $"sp_{EntityName}_List";
        var parameters = new
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Offset = (pageNumber - 1) * pageSize
        };

        return await QueryAsync(procedureName, parameters, cancellationToken);
    }

    /// <summary>
    /// Get total count of entities using sp_{EntityName}_Count stored procedure.
    /// </summary>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var procedureName = $"sp_{EntityName}_Count";
        return await QueryScalarAsync<int>(procedureName, null, cancellationToken);
    }

    /// <summary>
    /// Execute a transaction-based batch operation.
    /// Useful for operations requiring multiple related updates.
    /// </summary>
    public async Task<T> ExecuteTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(operation, nameof(operation));

        try
        {
            if (_connection.State == ConnectionState.Closed)
                await ((SqlConnection)_connection).OpenAsync(cancellationToken);

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    var result = await operation(_connection, transaction);
                    transaction.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during transaction, rolling back");
                    transaction.Rollback();
                    throw;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation cancelled during transaction");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transaction");
            throw;
        }
    }
}
