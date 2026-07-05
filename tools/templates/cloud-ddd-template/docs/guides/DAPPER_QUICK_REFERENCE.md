# Dapper Quick Reference

## Registration

```csharp
// Program.cs
builder.Services.AddDapperPersistence(builder.Configuration);
```

## Dependency Injection

```csharp
public class MyHandler
{
    public MyHandler(IDapperRepository<Order, OrderId> repo) { }
}
```

## Common Operations

### Get Single by ID
```csharp
var order = await _repo.GetByIdAsync(orderId, ct);
```
**Executes:** `sp_Order_GetById @Id={orderId}`

### Get Paginated List
```csharp
var orders = await _repo.ListAsync(pageNumber: 1, pageSize: 25, ct);
```
**Executes:** `sp_Order_List @PageNumber=1 @PageSize=25 @Offset=0`

### Get Count
```csharp
var total = await _repo.CountAsync(ct);
```
**Executes:** `sp_Order_Count`

### Get Custom Query
```csharp
var orders = await _repo.QueryAsync(
    "sp_Order_GetByCustomer", 
    new { CustomerId = customerId },
    ct);
```

### Get Scalar Value
```csharp
var count = await _repo.QueryScalarAsync<int>(
    "sp_Order_GetPendingCount",
    null,
    ct);
```

### Create/Update/Delete
```csharp
var affectedRows = await _repo.ExecuteAsync(
    "sp_Order_Insert",
    new { OrderId = id, CustomerId = cid, Status = "Pending" },
    ct);
```

### Multi-Step Transaction
```csharp
await _repo.ExecuteTransactionAsync(async (connection, transaction) =>
{
    // Step 1: Insert order
    await connection.ExecuteAsync(
        "sp_Order_Insert", 
        orderParams,
        transaction,
        commandType: CommandType.StoredProcedure);

    // Step 2: Insert items
    foreach (var item in items)
    {
        await connection.ExecuteAsync(
            "sp_OrderItem_Insert",
            itemParams,
            transaction,
            commandType: CommandType.StoredProcedure);
    }

    return affectedRows;
}, ct);
```

## Stored Procedure Naming

| Operation | Pattern | Example |
|-----------|---------|---------|
| Get by ID | `sp_{Entity}_GetById` | `sp_Order_GetById` |
| List | `sp_{Entity}_List` | `sp_Order_List` |
| Count | `sp_{Entity}_Count` | `sp_Order_Count` |
| Create | `sp_{Entity}_Insert` | `sp_Order_Insert` |
| Update | `sp_{Entity}_Update` | `sp_Order_Update` |
| Delete | `sp_{Entity}_Delete` | `sp_Order_Delete` |

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=true;"
  }
}
```

### Program.cs Options
```csharp
// Option 1: Simple (reads ConnectionStrings:DefaultConnection)
builder.Services.AddDapperPersistence(builder.Configuration);

// Option 2: Custom connection string
builder.Services.AddDapperPersistence(
    "Server=.;Database=MyDb;Trusted_Connection=true;");

// Option 3: Advanced configuration
builder.Services.AddDapperPersistenceWithConfiguration(config =>
{
    config.ConnectionString = builder.Configuration
        .GetConnectionString("DefaultConnection");
    config.CommandTimeout = 60;
    config.MaxPoolSize = 100;
    config.LogSqlCommands = true;
});
```

## Testing

### Mock Repository
```csharp
var mockRepo = new Mock<IDapperRepository<Order, OrderId>>();

mockRepo
    .Setup(x => x.GetByIdAsync(
        It.IsAny<OrderId>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(expectedOrder);

var handler = new GetOrderHandler(mockRepo.Object);
var result = await handler.Handle(query, CancellationToken.None);
```

### Mock Custom Query
```csharp
mockRepo
    .Setup(x => x.QueryAsync(
        "sp_Order_GetByCustomer",
        It.IsAny<object>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(expectedOrders);
```

## Common SQL Patterns

### Get by ID
```sql
CREATE PROCEDURE sp_Order_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SELECT * FROM Orders WHERE OrderId = @Id AND IsDeleted = 0;
END;
```

### Paginated List
```sql
CREATE PROCEDURE sp_Order_List
    @PageNumber INT = 1,
    @PageSize INT = 25,
    @Offset INT = 0
AS
BEGIN
    SELECT * FROM Orders 
    WHERE IsDeleted = 0
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
```

### Count
```sql
CREATE PROCEDURE sp_Order_Count
AS
BEGIN
    SELECT COUNT(*) FROM Orders WHERE IsDeleted = 0;
END;
```

### Insert
```sql
CREATE PROCEDURE sp_Order_Insert
    @OrderId UNIQUEIDENTIFIER,
    @CustomerId UNIQUEIDENTIFIER,
    @Status NVARCHAR(50),
    @TotalAmount DECIMAL(18,2)
AS
BEGIN
    INSERT INTO Orders (OrderId, CustomerId, Status, TotalAmount, CreatedAt, IsDeleted)
    VALUES (@OrderId, @CustomerId, @Status, @TotalAmount, GETUTCDATE(), 0);
    SELECT @@ROWCOUNT;
END;
```

### Update
```sql
CREATE PROCEDURE sp_Order_Update
    @OrderId UNIQUEIDENTIFIER,
    @Status NVARCHAR(50)
AS
BEGIN
    UPDATE Orders SET Status = @Status WHERE OrderId = @OrderId;
    SELECT @@ROWCOUNT;
END;
```

### Soft Delete
```sql
CREATE PROCEDURE sp_Order_Delete
    @OrderId UNIQUEIDENTIFIER
AS
BEGIN
    UPDATE Orders SET IsDeleted = 1 WHERE OrderId = @OrderId;
    SELECT @@ROWCOUNT;
END;
```

## Logging

### Enable Debug Logging
```json
{
  "Logging": {
    "LogLevel": {
      "Templates.Infrastructure.Persistence.Dapper": "Debug"
    }
  }
}
```

### Log Output Examples
```
[DBG] Executing stored procedure: sp_Order_GetById
[INF] Order a1b2c3d4-e5f6-4789-1011-121314151617 created successfully
[WRN] Operation cancelled while executing sp_Order_List
[ERR] Error executing stored procedure sp_Order_Insert: Connection timeout
```

## Performance Tips

1. **Create Indexes** on filter columns
   ```sql
   CREATE INDEX IX_Orders_CustomerId ON Orders(CustomerId);
   CREATE INDEX IX_Orders_Status ON Orders(Status);
   ```

2. **Use Execution Plans** to optimize queries
   ```sql
   SET STATISTICS IO ON;
   EXEC sp_Order_List @PageNumber=1, @PageSize=25;
   ```

3. **Batch Operations** with transactions
   ```csharp
   await _repo.ExecuteTransactionAsync(async (conn, trans) => {
       // Multiple ops here
   });
   ```

4. **Connection String Pooling**
   ```
   Min Pool Size=5; Max Pool Size=100;
   ```

5. **Monitor with Application Insights**
   ```csharp
   services.AddApplicationInsightsTelemetry();
   ```

## Troubleshooting

| Error | Cause | Solution |
|-------|-------|----------|
| `Invalid object name` | Stored proc doesn't exist | Run SQL script to create procedure |
| `Parameter count mismatch` | Parameter names don't match | Check anonymous object property names |
| `Connection timeout` | DB unreachable or slow query | Verify connection string, optimize query |
| `Cannot insert NULL` | Missing required parameter | Ensure all parameters passed |
| `Invalid cast` | Data type mismatch | Verify stored proc return type |

## Cheat Sheet

```csharp
// Inject
IDapperRepository<Order, OrderId> _repo

// Single
await _repo.GetByIdAsync(id, ct)

// List
await _repo.ListAsync(1, 25, ct)

// Count
await _repo.CountAsync(ct)

// Custom query
await _repo.QueryAsync("proc_name", @params, ct)

// Scalar
await _repo.QueryScalarAsync<int>("proc_name", null, ct)

// Execute
await _repo.ExecuteAsync("proc_name", @params, ct)

// Transaction
await _repo.ExecuteTransactionAsync(async (conn, trans) => {...}, ct)

// Direct executor
IStoredProcedureExecutor _executor
await _executor.QueryAsync<T>("proc_name", @params, ct)
await _executor.ExecuteAsync("proc_name", @params, ct)
```

## Resources

- **Guide**: [DAPPER_GUIDE.md](DAPPER_GUIDE.md)
- **Implementation**: [DAPPER_IMPLEMENTATION.md](DAPPER_IMPLEMENTATION.md)
- **Dapper Docs**: https://github.com/DapperLib/Dapper
- **SQL Server**: https://docs.microsoft.com/sql/t-sql/

---

**Version:** 1.0  
**Target Performance:** 1000+ TPS  
**Status:** Production Ready ✅
