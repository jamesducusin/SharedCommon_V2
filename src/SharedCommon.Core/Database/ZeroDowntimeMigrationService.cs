using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedCommon.Observability;

namespace SharedCommon.Core.Database;

/// <summary>
/// Service for managing zero-downtime database schema migrations.
/// Implements 4-phase migration pattern: Add → Deploy (dual-write) → Migrate → Remove
/// </summary>
public class ZeroDowntimeMigrationService : IZeroDowntimeMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<ZeroDowntimeMigrationService> _logger;
    private readonly ITelemetryService _telemetry;

    public ZeroDowntimeMigrationService(
        string connectionString,
        ILogger<ZeroDowntimeMigrationService> logger,
        ITelemetryService telemetry)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    /// <summary>
    /// Adds a new nullable column without locking the table.
    /// Phase 1 of zero-downtime migration.
    /// </summary>
    public async Task<MigrationResult> AddNullableColumnAsync(
        string tableName,
        string columnName,
        string columnType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
        if (string.IsNullOrEmpty(columnType)) throw new ArgumentNullException(nameof(columnType));

        using var scope = _telemetry.StartOperation("AddNullableColumn", "database");
        scope.SetTag("table", tableName);
        scope.SetTag("column", columnName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if column already exists
            var checkColumnSql = $@"
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";

            using var checkCmd = new SqlCommand(checkColumnSql, connection);
            var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);

            if (exists != null)
            {
                _logger.LogWarning("Column {Column} already exists in {Table}", columnName, tableName);
                return new MigrationResult { Success = true, IsIdempotent = true };
            }

            // Add column with ONLINE=ON (SQL Server 2016+)
            var addColumnSql = $@"
                ALTER TABLE [{tableName}] 
                ADD [{columnName}] {columnType} NULL
                WITH (ONLINE=ON);";

            using var cmd = new SqlCommand(addColumnSql, connection)
            {
                CommandTimeout = 3600 // 1 hour
            };

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Column {Column} added to {Table} in {DurationMs}ms",
                columnName, tableName, stopwatch.ElapsedMilliseconds);

            _telemetry.RecordMetric("migration.column_added", 1, new()
            {
                { "table", tableName },
                { "duration_ms", stopwatch.ElapsedMilliseconds.ToString() }
            });

            scope.MarkSucceeded();
            return new MigrationResult
            {
                Success = true,
                DurationMs = stopwatch.ElapsedMilliseconds,
                RowsAffected = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding column {Column} to {Table}", columnName, tableName);
            scope.RecordException(ex);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Renames a column without locking the table.
    /// Phase 1 of zero-downtime migration (alternatively: add new + backfill + drop old).
    /// </summary>
    public async Task<MigrationResult> RenameColumnAsync(
        string tableName,
        string oldColumnName,
        string newColumnName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(oldColumnName)) throw new ArgumentNullException(nameof(oldColumnName));
        if (string.IsNullOrEmpty(newColumnName)) throw new ArgumentNullException(nameof(newColumnName));

        using var scope = _telemetry.StartOperation("RenameColumn", "database");
        scope.SetTag("table", tableName);
        scope.SetTag("from", oldColumnName);
        scope.SetTag("to", newColumnName);

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var renameSql = $"EXEC sp_rename '{tableName}.{oldColumnName}', '{newColumnName}', 'COLUMN';";

            using var cmd = new SqlCommand(renameSql, connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Column {OldName} renamed to {NewName} in {Table}",
                oldColumnName, newColumnName, tableName);

            scope.MarkSucceeded();
            return new MigrationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming column {OldName} to {NewName}", oldColumnName, newColumnName);
            scope.RecordException(ex);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Backfills data in a new column in batches to avoid long locks.
    /// Phase 2-3 of zero-downtime migration.
    /// </summary>
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

        using var scope = _telemetry.StartOperation("BackfillColumn", "database");
        scope.SetTag("table", tableName);
        scope.SetTag("batch_size", batchSize.ToString());

        var totalRows = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get total rows to process
            var countSql = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{targetColumn}] IS NULL";
            using var countCmd = new SqlCommand(countSql, connection);
            var totalCount = (int)await countCmd.ExecuteScalarAsync(cancellationToken);

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

                using var updateCmd = new SqlCommand(updateSql, connection)
                {
                    CommandTimeout = 600 // 10 minutes per batch
                };

                var affectedRows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                totalRows += affectedRows;

                _telemetry.RecordMetric("migration.rows_backfilled", affectedRows, new()
                {
                    { "table", tableName },
                    { "total_progress", ((double)totalRows / totalCount * 100).ToString("F1") }
                });

                scope.SetTag("progress_percent", ((double)totalRows / totalCount * 100).ToString("F1"));

                if (affectedRows == 0)
                    break;

                // Small delay between batches to avoid table lock
                await Task.Delay(100, cancellationToken);

                _logger.LogInformation(
                    "Backfill progress: {Processed}/{Total} rows ({Percent}%)",
                    totalRows, totalCount, ((double)totalRows / totalCount * 100).ToString("F1"));
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Backfilled {RowCount} rows in {Table} in {DurationSec}s",
                totalRows, tableName, stopwatch.Elapsed.TotalSeconds);

            scope.MarkSucceeded();
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
            scope.RecordException(ex);
            return new MigrationResult { Success = false, Error = ex.Message, RowsAffected = totalRows };
        }
    }

    /// <summary>
    /// Drops a column safely after successful migration and new code deployment.
    /// Phase 4 of zero-downtime migration.
    /// </summary>
    public async Task<MigrationResult> DropColumnAsync(
        string tableName,
        string columnName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));

        using var scope = _telemetry.StartOperation("DropColumn", "database");
        scope.SetTag("table", tableName);
        scope.SetTag("column", columnName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if column exists
            var checkColumnSql = $@"
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";

            using var checkCmd = new SqlCommand(checkColumnSql, connection);
            var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);

            if (exists == null)
            {
                _logger.LogWarning("Column {Column} does not exist in {Table}", columnName, tableName);
                return new MigrationResult { Success = true, IsIdempotent = true };
            }

            // Drop column with ONLINE=ON (SQL Server 2016+)
            var dropColumnSql = $@"
                ALTER TABLE [{tableName}] 
                DROP COLUMN [{columnName}]
                WITH (ONLINE=ON);";

            using var cmd = new SqlCommand(dropColumnSql, connection)
            {
                CommandTimeout = 3600
            };

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Column {Column} dropped from {Table} in {DurationMs}ms",
                columnName, tableName, stopwatch.ElapsedMilliseconds);

            scope.MarkSucceeded();
            return new MigrationResult
            {
                Success = true,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping column {Column} from {Table}", columnName, tableName);
            scope.RecordException(ex);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Creates an index without blocking writes.
    /// </summary>
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

        using var scope = _telemetry.StartOperation("CreateIndex", "database");
        scope.SetTag("table", tableName);
        scope.SetTag("index", indexName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var uniqueClause = isUnique ? "UNIQUE " : "";
            var createIndexSql = $@"
                CREATE {uniqueClause}NONCLUSTERED INDEX [{indexName}]
                ON [{tableName}] ({columnNames})
                WITH (ONLINE=ON);";

            using var cmd = new SqlCommand(createIndexSql, connection)
            {
                CommandTimeout = 3600
            };

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Index {IndexName} created on {Table} in {DurationMs}ms",
                indexName, tableName, stopwatch.ElapsedMilliseconds);

            scope.MarkSucceeded();
            return new MigrationResult
            {
                Success = true,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName} on {Table}", indexName, tableName);
            scope.RecordException(ex);
            return new MigrationResult { Success = false, Error = ex.Message };
        }
    }
}

public interface IZeroDowntimeMigrationService
{
    Task<MigrationResult> AddNullableColumnAsync(string tableName, string columnName, string columnType, CancellationToken cancellationToken = default);
    Task<MigrationResult> RenameColumnAsync(string tableName, string oldColumnName, string newColumnName, CancellationToken cancellationToken = default);
    Task<MigrationResult> BackfillColumnAsync(string tableName, string sourceColumn, string targetColumn, int batchSize = 1000, CancellationToken cancellationToken = default);
    Task<MigrationResult> DropColumnAsync(string tableName, string columnName, CancellationToken cancellationToken = default);
    Task<MigrationResult> CreateIndexAsync(string tableName, string indexName, string columnNames, bool isUnique = false, CancellationToken cancellationToken = default);
}

public class MigrationResult
{
    public bool Success { get; set; }
    public bool IsIdempotent { get; set; }
    public long DurationMs { get; set; }
    public int RowsAffected { get; set; }
    public string? Error { get; set; }
}
