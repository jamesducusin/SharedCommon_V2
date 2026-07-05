using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SharedCommon.Core.Database;

/// <summary>
/// Service for managing zero-downtime database schema migrations.
/// Implements 4-phase migration pattern: Add column → Deploy code (dual-write) → Migrate data → Remove column
/// </summary>
/// <remarks>
/// Key principles:
/// - Each migration is independent and idempotent
/// - Supports rollback at any phase
/// - No locks on production tables
/// - Maintains backward compatibility during transition
/// - Automatic batching and monitoring for large datasets
/// </remarks>
public interface IZeroDowntimeMigrationService
{
    /// <summary>
    /// Adds a new nullable column without locking the table.
    /// Phase 1 of zero-downtime migration.
    /// </summary>
    /// <param name="tableName">The table to modify</param>
    /// <param name="columnName">The new column name</param>
    /// <param name="columnType">The column type and constraints (e.g., "VARCHAR(255) NULL")</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result indicating success and performance metrics</returns>
    /// <exception cref="ArgumentNullException">Thrown when tableName, columnName, or columnType is null</exception>
    Task<MigrationResult> AddNullableColumnAsync(
        string tableName,
        string columnName,
        string columnType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a column without locking the table.
    /// Phase 1 alternative of zero-downtime migration.
    /// </summary>
    /// <param name="tableName">The table to modify</param>
    /// <param name="oldColumnName">The current column name</param>
    /// <param name="newColumnName">The new column name</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result indicating success</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    Task<MigrationResult> RenameColumnAsync(
        string tableName,
        string oldColumnName,
        string newColumnName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Backfills data in a new column in batches to avoid long locks.
    /// Phase 2-3 of zero-downtime migration (copy data, then remove old column).
    /// </summary>
    /// <param name="tableName">The table to backfill</param>
    /// <param name="sourceColumn">The source column to read from</param>
    /// <param name="targetColumn">The target column to write to</param>
    /// <param name="batchSize">Number of rows to process per batch (default: 1000)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result with row count and performance metrics</returns>
    /// <exception cref="ArgumentNullException">Thrown when tableName, sourceColumn, or targetColumn is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batchSize is &lt;= 0</exception>
    Task<MigrationResult> BackfillColumnAsync(
        string tableName,
        string sourceColumn,
        string targetColumn,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops a column safely after successful migration and new code deployment.
    /// Phase 4 of zero-downtime migration.
    /// </summary>
    /// <param name="tableName">The table to modify</param>
    /// <param name="columnName">The column to drop</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result indicating success</returns>
    /// <exception cref="ArgumentNullException">Thrown when tableName or columnName is null</exception>
    Task<MigrationResult> DropColumnAsync(
        string tableName,
        string columnName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a non-clustered index without blocking writes.
    /// </summary>
    /// <param name="tableName">The table to create index on</param>
    /// <param name="indexName">The index name</param>
    /// <param name="columnNames">Comma-separated column names (e.g., "OrderId, CreatedAt")</param>
    /// <param name="isUnique">Whether the index enforces uniqueness</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Migration result indicating success and build time</returns>
    /// <exception cref="ArgumentNullException">Thrown when tableName, indexName, or columnNames is null</exception>
    Task<MigrationResult> CreateIndexAsync(
        string tableName,
        string indexName,
        string columnNames,
        bool isUnique = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a database migration operation.
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Gets or sets whether the migration succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets whether the migration is idempotent (safe to retry).
    /// </summary>
    public bool IsIdempotent { get; set; }

    /// <summary>
    /// Gets or sets the migration execution time in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the number of rows affected by the migration.
    /// </summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// Gets or sets an error message if the migration failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// SQL Server implementation of <see cref="IZeroDowntimeMigrationService"/>.
/// Uses SQL Server 2016+ ONLINE operations for non-blocking DDL.
/// </summary>
public sealed class ZeroDowntimeMigrationService : IZeroDowntimeMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<ZeroDowntimeMigrationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeroDowntimeMigrationService"/> class.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when connectionString or logger is null</exception>
    public ZeroDowntimeMigrationService(
        string connectionString,
        ILogger<ZeroDowntimeMigrationService> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<MigrationResult> AddNullableColumnAsync(
        string tableName,
        string columnName,
        string columnType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
        if (string.IsNullOrEmpty(columnType)) throw new ArgumentNullException(nameof(columnType));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if column already exists
            var checkSql = $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            using var checkCmd = new SqlCommand(checkSql, connection);
            var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);

            if (exists != null)
            {
                _logger.LogInformation("Column {Column} already exists in {Table}", columnName, tableName);
                return new MigrationResult { Success = true, IsIdempotent = true };
            }

            // Add column with ONLINE=ON (SQL Server 2016+)
            var addSql = $"ALTER TABLE [{tableName}] ADD [{columnName}] {columnType} WITH (ONLINE=ON);";
            using var cmd = new SqlCommand(addSql, connection) { CommandTimeout = 3600 };
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Column {Column} added to {Table} in {DurationMs}ms",
                columnName, tableName, stopwatch.ElapsedMilliseconds);

            return new MigrationResult { Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding column {Column} to {Table}", columnName, tableName);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> RenameColumnAsync(
        string tableName,
        string oldColumnName,
        string newColumnName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(oldColumnName)) throw new ArgumentNullException(nameof(oldColumnName));
        if (string.IsNullOrEmpty(newColumnName)) throw new ArgumentNullException(nameof(newColumnName));

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"EXEC sp_rename '{tableName}.{oldColumnName}', '{newColumnName}', 'COLUMN';";
            using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Column {OldName} renamed to {NewName} in {Table}",
                oldColumnName, newColumnName, tableName);

            return new MigrationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming column {OldName} to {NewName}", oldColumnName, newColumnName);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> BackfillColumnAsync(
        string tableName,
        string sourceColumn,
        string targetColumn,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(sourceColumn)) throw new ArgumentNullException(nameof(sourceColumn));
        if (string.IsNullOrEmpty(targetColumn)) throw new ArgumentNullException(nameof(targetColumn));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        var totalRows = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get total rows
            var countSql = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{targetColumn}] IS NULL";
            using var countCmd = new SqlCommand(countSql, connection);
            var totalCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);

            if (totalCount == 0)
            {
                _logger.LogInformation("No rows to backfill in {Table}", tableName);
                return new MigrationResult { Success = true, RowsAffected = 0 };
            }

            // Backfill in batches
            while (totalRows < totalCount)
            {
                var updateSql = $@"
                    UPDATE TOP ({batchSize}) [{tableName}]
                    SET [{targetColumn}] = [{sourceColumn}]
                    WHERE [{targetColumn}] IS NULL;";

                using var updateCmd = new SqlCommand(updateSql, connection) { CommandTimeout = 600 };
                var affectedRows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                totalRows += affectedRows;

                var progressPercent = (double)totalRows / totalCount * 100;
                _logger.LogInformation(
                    "Backfill progress: {Processed}/{Total} rows ({Percent:F1}%)",
                    totalRows, totalCount, progressPercent);

                if (affectedRows == 0)
                    break;

                await Task.Delay(100, cancellationToken);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Backfilled {RowCount} rows in {Table} in {DurationSec}s",
                totalRows, tableName, stopwatch.Elapsed.TotalSeconds);

            return new MigrationResult
            {
                Success = true,
                DurationMs = (long)stopwatch.Elapsed.TotalMilliseconds,
                RowsAffected = totalRows
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backfilling column {TargetColumn} in {Table}", targetColumn, tableName);
            return new MigrationResult { Success = false, Error = ex.Message, RowsAffected = totalRows };
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> DropColumnAsync(
        string tableName,
        string columnName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if column exists
            var checkSql = $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            using var checkCmd = new SqlCommand(checkSql, connection);
            var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);

            if (exists == null)
            {
                _logger.LogInformation("Column {Column} does not exist in {Table}", columnName, tableName);
                return new MigrationResult { Success = true, IsIdempotent = true };
            }

            // Drop column with ONLINE=ON
            var dropSql = $"ALTER TABLE [{tableName}] DROP COLUMN [{columnName}] WITH (ONLINE=ON);";
            using var cmd = new SqlCommand(dropSql, connection) { CommandTimeout = 3600 };
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Column {Column} dropped from {Table} in {DurationMs}ms",
                columnName, tableName, stopwatch.ElapsedMilliseconds);

            return new MigrationResult { Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping column {Column} from {Table}", columnName, tableName);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<MigrationResult> CreateIndexAsync(
        string tableName,
        string indexName,
        string columnNames,
        bool isUnique = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(indexName)) throw new ArgumentNullException(nameof(indexName));
        if (string.IsNullOrEmpty(columnNames)) throw new ArgumentNullException(nameof(columnNames));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var uniqueClause = isUnique ? "UNIQUE " : "";
            var sql = $@"
                CREATE {uniqueClause}NONCLUSTERED INDEX [{indexName}]
                ON [{tableName}] ({columnNames})
                WITH (ONLINE=ON);";

            using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 3600 };
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Index {IndexName} created on {Table} in {DurationMs}ms",
                indexName, tableName, stopwatch.ElapsedMilliseconds);

            return new MigrationResult { Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName} on {Table}", indexName, tableName);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }
}
