namespace Templates.Infrastructure.Persistence;

using System.Data;
using System.Data.SqlClient;
using Templates.Infrastructure.Common;

/// <summary>
/// Dapper-based implementation of the Unit of Work pattern.
/// Manages database transactions using SQL Server connections.
/// </summary>
public sealed class UnitOfWork(IDbConnection connection) : IUnitOfWork
{
    private IDbTransaction? _transaction;

    /// <summary>
    /// Saves all pending changes to the database.
    /// Note: With Dapper, changes are persisted immediately via stored procedures.
    /// This method is provided for API compatibility.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // With Dapper, changes are persisted immediately through stored procedures
        // No need to explicitly save changes
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (connection.State == ConnectionState.Closed)
                await ((SqlConnection)connection).OpenAsync(cancellationToken).ConfigureAwait(false);

            _transaction = connection.BeginTransaction();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to begin database transaction", ex);
        }
    }

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _transaction?.Commit();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to commit database transaction", ex);
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _transaction?.Rollback();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to rollback database transaction", ex);
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the unit of work and its resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _transaction?.Dispose();
        
        if (connection?.State == ConnectionState.Open)
        {
            if (connection is SqlConnection sqlConnection)
                await sqlConnection.CloseAsync().ConfigureAwait(false);
            else
                connection.Close();
        }

        connection?.Dispose();
    }
}
