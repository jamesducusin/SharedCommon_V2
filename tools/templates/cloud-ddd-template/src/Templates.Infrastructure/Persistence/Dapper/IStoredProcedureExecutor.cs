using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Templates.Infrastructure.Persistence.Dapper;

/// <summary>
/// Convenience wrapper for executing stored procedures with Dapper.
/// Provides a fluent, EF Core-like interface while leveraging Dapper's performance.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <summary>
    /// Execute a stored procedure and return a single result.
    /// </summary>
    Task<T?> QuerySingleAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a stored procedure and return multiple results.
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a stored procedure and return a scalar value.
    /// </summary>
    Task<T> QueryScalarAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a stored procedure for modifications (insert/update/delete).
    /// Returns number of affected rows.
    /// </summary>
    Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute multiple stored procedures in a single transaction.
    /// </summary>
    Task<T> ExecuteTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> operation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of stored procedure executor using Dapper.
/// </summary>
public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly IDbConnection _connection;
    private readonly ILogger<StoredProcedureExecutor> _logger;

    public StoredProcedureExecutor(IDbConnection connection, ILogger<StoredProcedureExecutor> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> QuerySingleAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing stored procedure: {ProcedureName}", procedureName);

            var result = await _connection.QueryFirstOrDefaultAsync<T?>(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing stored procedure: {ProcedureName}", procedureName);

            var results = await _connection.QueryAsync<T>(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    public async Task<T> QueryScalarAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing scalar stored procedure: {ProcedureName}", procedureName);

            var result = await _connection.ExecuteScalarAsync<T>(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scalar stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    public async Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));

        try
        {
            _logger.LogDebug("Executing stored procedure: {ProcedureName}", procedureName);

            var affectedRows = await _connection.ExecuteAsync(
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 30);

            return affectedRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            throw;
        }
    }

    public async Task<T> ExecuteTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transaction");
            throw;
        }
    }
}
