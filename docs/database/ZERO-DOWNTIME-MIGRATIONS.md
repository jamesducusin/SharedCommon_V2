# Zero-Downtime Database Migrations

Guide for applying schema changes without service interruption.

---

## Core Principles

1. **Backwards Compatibility:** Old code must work with new schema
2. **Forwards Compatibility:** New code must work with old schema (during transition)
3. **Phased Approach:** Separate deployment from migration
4. **Rollback Capability:** Every change must be reversible

---

## Migration Strategy

### Phase 1: Add Column with Default (Backwards Compatible)

**Release V1: Deploy Code**

Old code continues to work (reads from existing column).

```sql
-- Migration V1__AddNewColumn.sql
ALTER TABLE Orders ADD OrderStatus NVARCHAR(50) DEFAULT 'Pending';

-- Add index if frequently queried
CREATE INDEX IX_Orders_Status ON Orders(OrderStatus);
```

**Deployment:**
```bash
helm upgrade templates-api helm/ \
  --values helm/values-prod.yaml \
  --set image.tag=v1.0
```

### Phase 2: Deploy Code Using New Column

**Release V2: Deploy Code That Reads/Writes New Column**

New code writes to new column. Reads from new column preferentially, falls back to old column if null.

```csharp
// V2 Code - Hybrid read/write
public async Task<Order> GetOrderAsync(Guid orderId)
{
    var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
    
    // Read from new column (preferred), fall back to old column
    var status = order.OrderStatus ?? order.LegacyOrderStatusCode;
    
    return new Order { Id = orderId, Status = status };
}

public async Task UpdateOrderAsync(Order order)
{
    var dbOrder = new DbOrder
    {
        Id = order.Id,
        OrderStatus = order.Status,  // Write to new column
        LegacyOrderStatusCode = order.Status  // Also write to old column
    };
    
    await _db.SaveChangesAsync();
}
```

**Deployment:**
```bash
helm upgrade templates-api helm/ \
  --values helm/values-prod.yaml \
  --set image.tag=v2.0
```

### Phase 3: Data Migration (Background Job)

**Before Rollout:** Run migration to populate new column from old column

```csharp
// DataMigrationService.cs
public class DataMigrationService
{
    public async Task MigrateOrderStatusAsync(CancellationToken ct)
    {
        const int batchSize = 1000;
        int migratedCount = 0;
        
        using var scope = _telemetry.StartOperation("MigrateOrderStatus", "migration");
        
        try
        {
            // Migrate in batches to avoid locking entire table
            while (true)
            {
                var ordersToMigrate = await _db.Orders
                    .Where(x => x.OrderStatus == null && x.LegacyOrderStatusCode != null)
                    .Take(batchSize)
                    .ToListAsync(ct);
                
                if (!ordersToMigrate.Any()) break;
                
                foreach (var order in ordersToMigrate)
                {
                    order.OrderStatus = order.LegacyOrderStatusCode;
                }
                
                await _db.SaveChangesAsync(ct);
                migratedCount += ordersToMigrate.Count;
                
                scope.SetTag("migration.progress", migratedCount);
                _telemetry.RecordMetric("migration.batch_completed", batchSize);
                
                // Small delay between batches to reduce load
                await Task.Delay(100, ct);
            }
            
            scope.SetTag("migration.total_rows", migratedCount);
            scope.MarkSucceeded();
        }
        catch (Exception ex)
        {
            scope.RecordException(ex);
            throw;
        }
    }
}

// Hosted service to run migration
public class DataMigrationHostedService : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        // Only run migration on pod starting (not on every request)
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = RunMigrationAsync(cts.Token);
        
        await Task.CompletedTask;
    }
    
    private async Task RunMigrationAsync(CancellationToken ct)
    {
        try
        {
            await _migrationService.MigrateOrderStatusAsync(ct);
            _logger.LogInformation("✓ Data migration completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Data migration failed - continuing anyway");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

**Verification:**
```sql
-- Check migration progress
SELECT 
    COUNT(*) AS TotalRows,
    COUNT(CASE WHEN OrderStatus IS NOT NULL THEN 1 END) AS MigratedRows,
    COUNT(CASE WHEN OrderStatus IS NULL THEN 1 END) AS PendingRows
FROM Orders;
```

### Phase 4: Remove Old Column (After Verification)

**Release V3: Deploy Code That Only Uses New Column**

Remove fallback logic, delete old column.

```csharp
// V3 Code - Only use new column
public async Task<Order> GetOrderAsync(Guid orderId)
{
    var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
    return new Order { Id = orderId, Status = order.OrderStatus };
}
```

```sql
-- Migration V3__RemoveOldColumn.sql
-- First remove index on old column if exists
DROP INDEX IF EXISTS IX_Orders_LegacyStatus;

-- Then remove column
ALTER TABLE Orders DROP COLUMN LegacyOrderStatusCode;
```

**Deployment:**
```bash
helm upgrade templates-api helm/ \
  --values helm/values-prod.yaml \
  --set image.tag=v3.0
```

---

## Migration Patterns

### Pattern 1: Add Column

**Steps:**
1. Add column with DEFAULT value
2. Deploy code using new column (with fallback)
3. Migrate data in background
4. Remove old column and fallback logic

**Timeline:** 3-4 releases over 2-4 weeks

### Pattern 2: Rename Column

**Steps:**
1. Add new column with same structure
2. Create trigger to sync old → new
3. Deploy code reading from new column
4. Migrate data
5. Remove trigger and old column

```sql
-- Step 1: Add new column and trigger
ALTER TABLE Orders ADD NewColumnName INT;

CREATE TRIGGER SyncOldToNewColumn
ON Orders
AFTER UPDATE, INSERT
AS
BEGIN
    UPDATE Orders 
    SET NewColumnName = inserted.OldColumnName
    FROM inserted
    WHERE Orders.Id = inserted.Id;
END;
```

### Pattern 3: Change Column Type

**Steps:**
1. Add new column with new type
2. Create computed column or trigger to convert
3. Deploy code using new column
4. Migrate data with conversion logic
5. Remove old column

```sql
-- Convert VARCHAR to INT
ALTER TABLE Orders ADD StatusCode INT;

-- Conversion (if possible)
UPDATE Orders SET StatusCode = 
    CASE OrderStatus
        WHEN 'Pending' THEN 1
        WHEN 'Completed' THEN 2
        WHEN 'Cancelled' THEN 3
    END;
```

### Pattern 4: Split Table

**Steps:**
1. Create new table with same/similar structure
2. Deploy code writing to both tables
3. Copy historical data to new table
4. Deploy code reading from new table (with fallback)
5. Remove trigger and old table

```sql
-- Create new Orders table (with new schema)
CREATE TABLE OrdersV2 (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OrderNumber NVARCHAR(50),
    Status NVARCHAR(50),
    -- New columns here
);

-- Create trigger to sync data
CREATE TRIGGER SyncOrdersV1ToV2
ON Orders
AFTER INSERT, UPDATE
AS
BEGIN
    MERGE OrdersV2 AS target
    USING inserted AS source
    ON target.Id = source.Id
    WHEN MATCHED THEN
        UPDATE SET OrderNumber = source.OrderNumber, Status = source.Status
    WHEN NOT MATCHED THEN
        INSERT (Id, OrderNumber, Status) VALUES (source.Id, source.OrderNumber, source.Status);
END;
```

---

## Rollback Strategy

### Before Migration (Quick Rollback)

```bash
# If new column not yet migrated, simple rollback
helm rollout undo deployment/templates-api
kubectl rollout status deployment/templates-api

# Column persists but is unused
# No data loss
```

### After Migration (Careful Rollback)

```bash
# Cannot remove column if rolled back
# Instead:
# 1. Deploy V2.5 that uses both columns again
# 2. Re-sync data to old column
# 3. Then rollback if needed

kubectl apply -f deployment-v2.5.yaml
# Wait for migration to re-populate old column
kubectl rollout undo deployment/templates-api
```

### Testing Rollback

```bash
# Before going to prod, test rollback locally
docker-compose down -v
docker-compose up -d

# Run migration script
./scripts/migrate-up.sh

# Verify data
docker-compose exec db sqlcmd -U sa -Q "SELECT * FROM Orders"

# Rollback
./scripts/migrate-down.sh

# Verify data still there
docker-compose exec db sqlcmd -U sa -Q "SELECT * FROM Orders"
```

---

## Checklist

### Before Migration

- [ ] Database backup taken
- [ ] Data migration script tested locally
- [ ] Rollback procedure documented and tested
- [ ] Stakeholders notified (maintenance window if needed)
- [ ] Load testing done (if large table)
- [ ] Index strategy planned

### During Migration

- [ ] Monitor database CPU/IO
- [ ] Watch application latency
- [ ] Check error rates
- [ ] Verify migration progress
- [ ] Have rollback ready

### After Migration

- [ ] Verify data integrity
- [ ] Check application logs
- [ ] Monitor performance
- [ ] Run smoke tests
- [ ] Document what was learned

---

## Performance Considerations

### Large Tables (> 1M rows)

```sql
-- Use batch processing with delays
DECLARE @batchSize INT = 10000;
DECLARE @offset INT = 0;

WHILE @offset < (SELECT COUNT(*) FROM Orders)
BEGIN
    UPDATE TOP (@batchSize) Orders
    SET NewColumn = OldColumn
    WHERE NewColumn IS NULL;
    
    SET @offset = @offset + @batchSize;
    
    -- Small delay to reduce load
    WAITFOR DELAY '00:00:01';
END;
```

### Hot Tables (High Transaction Volume)

```sql
-- Migration during low-traffic window
-- Or use online index rebuild
ALTER TABLE Orders
REBUILD
WITH (ONLINE = ON, ALLOW_ROW_LOCKS = ON);
```

### Lock Management

```sql
-- Monitor locks during migration
SELECT * FROM sys.dm_tran_locks;

-- Use isolation level to reduce locking
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

BEGIN TRANSACTION;
    UPDATE Orders SET NewColumn = OldColumn;
COMMIT;
```

---

## Verification

### Data Integrity

```sql
-- After migration, verify data
SELECT 
    COUNT(*) AS TotalRows,
    COUNT(*) - SUM(CASE WHEN NewColumn IS NOT NULL THEN 1 ELSE 0 END) AS NullValues,
    COUNT(DISTINCT NewColumn) AS DistinctValues
FROM Orders;

-- Check for mismatches
SELECT TOP 100 *
FROM Orders
WHERE NewColumn != OldColumn;  -- Should be 0 rows
```

### Performance Impact

```sql
-- Check query plan
SET STATISTICS IO ON;
SELECT * FROM Orders WHERE Status = 'Pending';
SET STATISTICS IO OFF;

-- Expected: Use new index on NewColumn
```

