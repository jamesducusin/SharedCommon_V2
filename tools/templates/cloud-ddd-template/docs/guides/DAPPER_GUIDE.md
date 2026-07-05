# Dapper Implementation Guide

## Overview

This template uses **Dapper** instead of Entity Framework Core to optimize for high-traffic scenarios (1000+ transactions per second). Dapper is a lightweight, high-performance micro-ORM that provides a convenient but best-practice way to work with SQL Server stored procedures.

## Architecture Philosophy

- **Performance First**: No LINQ overhead, direct stored procedure execution
- **Convenience**: Generic `IDapperRepository<TEntity, TId>` eliminates repetitive per-entity repository classes
- **Best Practices**: Connection pooling, parameterized queries, transaction support
- **Type Safety**: Full C# type safety with Dapper's mapping capabilities

## Key Components

### 1. IDapperRepository<TEntity, TId>

Generic interface defining all data access operations:

```csharp
public interface IDapperRepository<TEntity, TId> where TEntity : IEntity
{
    // Query single entity from stored procedure
    Task<TEntity?> QuerySingleAsync(string procedureName, object? parameters, CancellationToken ct);
    
    // Query multiple entities from stored procedure
    Task<IEnumerable<TEntity>> QueryAsync(string procedureName, object? parameters, CancellationToken ct);
    
    // Query scalar value (count, ID, etc.)
    Task<T> QueryScalarAsync<T>(string procedureName, object? parameters, CancellationToken ct);
    
    // Execute INSERT/UPDATE/DELETE
    Task<int> ExecuteAsync(string procedureName, object? parameters, CancellationToken ct);
    
    // Standard get by ID (sp_{EntityName}_GetById)
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct);
    
    // Paginated list (sp_{EntityName}_List)
    Task<IEnumerable<TEntity>> ListAsync(int pageNumber, int pageSize, CancellationToken ct);
    
    // Total count (sp_{EntityName}_Count)
    Task<int> CountAsync(CancellationToken ct);
    
    // Batch operations with transaction
    Task<T> ExecuteTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> operation, CancellationToken ct);
}
```

### 2. DapperRepository<TEntity, TId>

Concrete implementation with:
- Automatic connection pooling via `SqlConnection`
- Command buffering for performance
- Proper logging at all levels
- `CancellationToken` support throughout
- Transaction support for complex operations

### 3. IStoredProcedureExecutor

Convenience wrapper for executing arbitrary stored procedures without needing a repository.

### 4. Stored Procedure Naming Convention

All standard operations follow a consistent naming pattern:

| Operation | Procedure Name | Parameters |
|-----------|---|---|
| Get by ID | `sp_{EntityName}_GetById` | `@Id` |
| List (paginated) | `sp_{EntityName}_List` | `@PageNumber, @PageSize, @Offset` |
| Count | `sp_{EntityName}_Count` | None |
| Insert | `sp_{EntityName}_Insert` | Entity fields as parameters |
| Update | `sp_{EntityName}_Update` | `@Id` + fields to update |
| Delete | `sp_{EntityName}_Delete` | `@Id` |

## Setup Instructions

### 1. Register Dapper Services

In `Program.cs`:

```csharp
builder.Services.AddDapperPersistence(builder.Configuration);
```

Or with custom configuration:

```csharp
builder.Services.AddDapperPersistenceWithConfiguration(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    config.CommandTimeout = 60;
    config.MaxPoolSize = 100;
});
```

### 2. Create Stored Procedures

Example for Orders entity:

```sql
CREATE PROCEDURE sp_Order_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SELECT OrderId, CustomerId, Status, TotalAmount, CreatedAt
    FROM Orders
    WHERE OrderId = @Id AND IsDeleted = 0;
END;
```

See `StoredProcedures_Orders.sql` for complete examples.

### 3. Define Repository Interface (Optional)

For domain-driven design, create a domain-specific repository interface:

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct);
    Task<IEnumerable<Order>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct);
    Task<int> CreateAsync(Order order, CancellationToken ct);
    Task<int> UpdateAsync(Order order, CancellationToken ct);
}
```

### 4. Implement Repository Using Generic Dapper

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly IDapperRepository<Order, OrderId> _dapperRepository;
    private readonly IStoredProcedureExecutor _executor;

    public OrderRepository(
        IDapperRepository<Order, OrderId> dapperRepository,
        IStoredProcedureExecutor executor)
    {
        _dapperRepository = dapperRepository;
        _executor = executor;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
    {
        return await _dapperRepository.GetByIdAsync(id.Value, ct);
    }

    public async Task<IEnumerable<Order>> GetByCustomerAsync(
        CustomerId customerId, CancellationToken ct)
    {
        var parameters = new { CustomerId = customerId.Value };
        return await _dapperRepository.QueryAsync(
            "sp_Order_GetByCustomer", 
            parameters, 
            ct);
    }

    public async Task<int> CreateAsync(Order order, CancellationToken ct)
    {
        var parameters = new
        {
            OrderId = order.Id.Value,
            CustomerId = order.CustomerId.Value,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount
        };
        return await _dapperRepository.ExecuteAsync("sp_Order_Insert", parameters, ct);
    }
}
```

## Usage Patterns

### Simple Query

```csharp
// Get order by ID
var order = await _orderRepository.GetByIdAsync(new OrderId(Guid.NewGuid()), cancellationToken);
```

### Custom Stored Procedure

```csharp
// Call any stored procedure directly
var parameters = new { CustomerId = customerId.Value };
var orders = await _orderRepository.QueryAsync(
    "sp_Order_GetByCustomer", 
    parameters, 
    cancellationToken);
```

### Scalar Query

```csharp
// Get count
var totalCount = await _orderRepository.CountAsync(cancellationToken);

// Or custom scalar
var nextId = await _orderRepository.QueryScalarAsync<int>(
    "sp_Order_GetNextId", 
    null, 
    cancellationToken);
```

### Transaction with Multiple Operations

```csharp
var result = await _orderRepository.ExecuteTransactionAsync(
    async (connection, transaction) =>
    {
        // First operation
        await connection.ExecuteAsync(
            "sp_Order_Insert",
            orderParams,
            transaction,
            commandType: CommandType.StoredProcedure);

        // Second operation
        var itemResult = await connection.ExecuteAsync(
            "sp_OrderItem_Insert",
            itemParams,
            transaction,
            commandType: CommandType.StoredProcedure);

        return itemResult;
    },
    cancellationToken);
```

## Performance Optimizations

### Connection Pooling

- **Automatic**: `SqlConnection` handles connection pooling transparently
- **Pooling Parameters**: Default min=1, max=100 (configurable in `appsettings.json`)
- **Strategy**: Repository keeps connection open for the lifetime of the request

### Command Buffering

- Dapper enables command buffering automatically for high throughput
- Multiple small queries combined into single roundtrip

### Stored Procedures

- **No Dynamic SQL**: All queries use stored procedures (pre-compiled, optimized)
- **Indexes**: Create indexes on common filter columns (CustomerId, Status, CreatedAt)
- **Pagination**: Use OFFSET/FETCH for efficient pagination

### Monitoring

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Templates.Infrastructure.Persistence.Dapper": "Debug"
    }
  }
}
```

## Testing

### Mock IDbConnection and Dapper Calls

```csharp
[Fact]
public async Task GetByIdAsync_ReturnsOrder_WhenOrderExists()
{
    // Arrange
    var mockConnection = new Mock<IDbConnection>();
    var mockOrder = new Order { OrderId = Guid.NewGuid() };

    mockConnection
        .Setup(x => x.QueryFirstOrDefaultAsync<Order>(
            "sp_Order_GetById",
            It.IsAny<object>(),
            null,
            It.IsAny<int>(),
            CommandType.StoredProcedure))
        .ReturnsAsync(mockOrder);

    var repository = new DapperRepository<Order, OrderId>(
        mockConnection.Object,
        _loggerMock.Object);

    // Act
    var result = await repository.GetByIdAsync(mockOrder.OrderId, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(mockOrder.OrderId, result.OrderId);
}
```

### Integration Tests

Use a test database with actual stored procedures:

```csharp
[Collection("Database collection")]
public class OrderRepositoryIntegrationTests
{
    private readonly TestDatabaseFixture _fixture;

    public OrderRepositoryIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateOrder_StoresOrderSuccessfully()
    {
        // Arrange
        var connection = _fixture.CreateConnection();
        var repository = new DapperRepository<Order, OrderId>(connection, _loggerMock.Object);
        var order = Order.Create(customerId, items).Value;

        // Act
        var result = await repository.ExecuteAsync("sp_Order_Insert", orderParams);

        // Assert
        Assert.Equal(1, result); // 1 row affected
    }
}
```

## Migration from Entity Framework Core

### Before (EF Core)

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }
}
```

### After (Dapper)

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly IDapperRepository<Order, OrderId> _repository;

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id.Value, ct);
    }
}
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=TemplateDb;Trusted_Connection=true;"
  },
  "Dapper": {
    "CommandTimeout": 30,
    "MaxPoolSize": 100,
    "LogSqlCommands": false
  },
  "Logging": {
    "LogLevel": {
      "Templates.Infrastructure.Persistence.Dapper": "Information"
    }
  }
}
```

## Best Practices

1. **Always use stored procedures** - Don't build dynamic SQL
2. **Use the generic repository** - Don't create per-entity repository classes
3. **Follow naming convention** - `sp_{EntityName}_{Operation}`
4. **Use transactions for related operations** - `ExecuteTransactionAsync`
5. **Log appropriately** - Info for important operations, Debug for detailed trace
6. **Handle cancellation** - Pass `CancellationToken` throughout
7. **Validate parameters** - Use Guard clauses before execution
8. **Index properly** - Create indices on filter columns in stored procedures
9. **Monitor performance** - Enable logging and monitor execution times
10. **Test thoroughly** - Both unit tests (mocked) and integration tests (real DB)

## Troubleshooting

### Issue: "Invalid object name" when executing stored procedure

**Cause**: Stored procedure doesn't exist in database
**Solution**: Run `StoredProcedures_Orders.sql` to create procedures

### Issue: Connection timeout

**Cause**: Database server unreachable or stored procedure running too long
**Solution**: Check connection string, verify database is running, optimize stored procedure

### Issue: Parameter mapping errors

**Cause**: Stored procedure parameters don't match object properties
**Solution**: Use anonymous object with matching property names or use `[Column]` attributes

### Issue: CancellationToken not working

**Cause**: Long-running query not respecting cancellation
**Solution**: Implement timeout in stored procedure or use `CommandTimeout` property

## Performance Benchmarks

Typical performance on modern hardware (1000+ TPS target):

- **GetByIdAsync**: ~1-5ms (1 network roundtrip + index lookup)
- **QueryAsync (1000 rows)**: ~10-50ms (depends on data size)
- **ExecuteAsync (insert)**: ~2-10ms (depends on stored procedure complexity)
- **ExecuteTransactionAsync (10 operations)**: ~20-100ms

Monitor actual performance with application insights or similar monitoring tools.

## See Also

- [Dapper GitHub Repository](https://github.com/DapperLib/Dapper)
- [Dapper Documentation](https://www.learndapper.com/)
- [SQL Server Stored Procedures](https://docs.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql)
- [Entity Framework Core to Dapper Migration](https://www.codeproject.com/Articles/5272890/Migrating-from-Entity-Framework-Core-to-Dapper)
