# EF Core to Dapper Migration Checklist

## Pre-Migration Planning

- [ ] **Review Current Architecture**
  - [ ] Identify all DbContext classes
  - [ ] List all repository implementations
  - [ ] Document LINQ query patterns
  - [ ] Note transaction usage patterns

- [ ] **Database Analysis**
  - [ ] Review current database schema
  - [ ] Identify all tables and relationships
  - [ ] Document indexes and constraints
  - [ ] Plan index strategy for stored procs

- [ ] **Performance Baseline**
  - [ ] Record current response times
  - [ ] Document current TPS (transactions per second)
  - [ ] Note database CPU/memory usage
  - [ ] Identify slow queries

- [ ] **Team Preparation**
  - [ ] Review Dapper concepts (see DAPPER_GUIDE.md)
  - [ ] Learn stored procedure development
  - [ ] Assign roles (SQL specialist, C# lead, QA)
  - [ ] Set migration timeline

## Phase 1: Setup Infrastructure

- [ ] **Install Dapper NuGet Package**
  ```bash
  dotnet add package Dapper --version 2.0.123
  ```

- [ ] **Create Dapper Layer**
  - [ ] Copy `IDapperRepository.cs`
  - [ ] Copy `DapperRepository.cs`
  - [ ] Copy `IStoredProcedureExecutor.cs`
  - [ ] Copy `DapperServiceCollectionExtensions.cs`

- [ ] **Register Services**
  - [ ] Update `Program.cs` with `AddDapperPersistence()`
  - [ ] Remove EF Core registrations
  - [ ] Test application starts without errors

- [ ] **Verify Connection**
  - [ ] Check connection string in `appsettings.json`
  - [ ] Test database connectivity
  - [ ] Verify connection pooling is active

## Phase 2: Entity-by-Entity Migration

For each entity (Order, Customer, etc.):

### A. Create SQL Stored Procedures

- [ ] **Get by ID**
  ```sql
  CREATE PROCEDURE sp_{Entity}_GetById
      @Id UNIQUEIDENTIFIER
  AS BEGIN
      SELECT * FROM {Entity}s WHERE {Entity}Id = @Id AND IsDeleted = 0;
  END;
  ```

- [ ] **List (Paginated)**
  ```sql
  CREATE PROCEDURE sp_{Entity}_List
      @PageNumber INT = 1,
      @PageSize INT = 25,
      @Offset INT = 0
  AS BEGIN
      SELECT * FROM {Entity}s 
      WHERE IsDeleted = 0
      ORDER BY CreatedAt DESC
      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
  END;
  ```

- [ ] **Count**
  ```sql
  CREATE PROCEDURE sp_{Entity}_Count
  AS BEGIN
      SELECT COUNT(*) FROM {Entity}s WHERE IsDeleted = 0;
  END;
  ```

- [ ] **Insert**
  ```sql
  CREATE PROCEDURE sp_{Entity}_Insert
      @{Entity}Id UNIQUEIDENTIFIER,
      -- other parameters...
  AS BEGIN
      INSERT INTO {Entity}s (...)
      VALUES (...);
      SELECT @@ROWCOUNT;
  END;
  ```

- [ ] **Update**
  ```sql
  CREATE PROCEDURE sp_{Entity}_Update
      @{Entity}Id UNIQUEIDENTIFIER,
      -- other parameters...
  AS BEGIN
      UPDATE {Entity}s SET ... WHERE {Entity}Id = @{Entity}Id;
      SELECT @@ROWCOUNT;
  END;
  ```

- [ ] **Delete** (soft delete)
  ```sql
  CREATE PROCEDURE sp_{Entity}_Delete
      @{Entity}Id UNIQUEIDENTIFIER
  AS BEGIN
      UPDATE {Entity}s SET IsDeleted = 1 WHERE {Entity}Id = @{Entity}Id;
      SELECT @@ROWCOUNT;
  END;
  ```

- [ ] **Create Indexes**
  ```sql
  CREATE INDEX IX_{Entity}_Status ON {Entity}s(Status);
  CREATE INDEX IX_{Entity}_CustomerId ON {Entity}s(CustomerId);
  ```

- [ ] **Test Procedures**
  - [ ] Execute each manually in SQL Server Management Studio
  - [ ] Verify results match expected output
  - [ ] Check performance with STATISTICS IO

### B. Create Domain Repository Interface

- [ ] **Define Interface**
  ```csharp
  public interface I{Entity}Repository
  {
      Task<{Entity}?> GetByIdAsync({Entity}Id id, CancellationToken ct);
      Task<IEnumerable<{Entity}>> ListAsync(int page, int size, CancellationToken ct);
      Task<int> CountAsync(CancellationToken ct);
      // ... other domain-specific methods
  }
  ```

- [ ] **Implement Using Generic Repository**
  ```csharp
  public class {Entity}Repository : I{Entity}Repository
  {
      private readonly IDapperRepository<{Entity}, {Entity}Id> _dapperRepo;
      
      public {Entity}Repository(IDapperRepository<{Entity}, {Entity}Id> repo)
      {
          _dapperRepo = repo;
      }
      
      public Task<{Entity}?> GetByIdAsync({Entity}Id id, CancellationToken ct)
          => _dapperRepo.GetByIdAsync(id.Value, ct);
      
      // ... implement other methods
  }
  ```

### C. Migrate Handlers/Services

- [ ] **Update Dependency Injection**
  - [ ] Replace EF Core DbContext with I{Entity}Repository
  - [ ] Update constructor parameters
  - [ ] Remove DbContext usage

- [ ] **Update Handler Code**
  - [ ] Replace `await context.{Entity}s.FirstOrDefaultAsync()`
  - [ ] Replace `await context.{Entity}s.ToListAsync()`
  - [ ] Replace `context.{Entity}s.Add(entity)`
  - [ ] Replace `await context.SaveChangesAsync()`

- [ ] **Handle Transactions**
  - [ ] Replace `context.Database.BeginTransactionAsync()`
  - [ ] Use `_repository.ExecuteTransactionAsync()`

### D. Update Tests

- [ ] **Unit Tests**
  - [ ] Update mocks from `DbContext` to `IDapperRepository<T, TId>`
  - [ ] Mock stored procedure calls
  - [ ] Update test assertions

- [ ] **Integration Tests**
  - [ ] Create test database with stored procedures
  - [ ] Use real `IDapperRepository` with test connection
  - [ ] Verify stored procedures work correctly

### E. Verify and Validate

- [ ] **Functional Testing**
  - [ ] Run application and verify entity operations
  - [ ] Test all CRUD operations
  - [ ] Test pagination
  - [ ] Test filtering/sorting

- [ ] **Performance Testing**
  - [ ] Measure single entity retrieval time
  - [ ] Measure list query performance
  - [ ] Compare with EF Core baseline
  - [ ] Verify TPS improvement

- [ ] **Error Handling**
  - [ ] Test with invalid IDs
  - [ ] Test database connection failures
  - [ ] Test timeout scenarios
  - [ ] Verify proper error logging

## Phase 3: Cleanup

- [ ] **Remove EF Core**
  - [ ] Delete old `{Entity}Repository` classes
  - [ ] Delete `ApplicationDbContext`
  - [ ] Delete EF Core DbSets
  - [ ] Remove EF Core NuGet package (if not used elsewhere)

- [ ] **Remove Migrations**
  - [ ] Delete `Migrations` folder
  - [ ] Remove migration code
  - [ ] Document database initialization process

- [ ] **Update Documentation**
  - [ ] Update README.md to mention Dapper
  - [ ] Document stored procedure naming convention
  - [ ] Update deployment guide
  - [ ] Add troubleshooting section

- [ ] **Code Review**
  - [ ] Peer review all changes
  - [ ] Verify naming conventions followed
  - [ ] Check error handling completeness
  - [ ] Review performance optimizations

## Phase 4: Testing & Validation

### A. Unit Testing
- [ ] All handlers tested with mocked repositories
- [ ] Edge cases covered
- [ ] Error scenarios tested
- [ ] Code coverage ≥ 80%

### B. Integration Testing
- [ ] Stored procedures work correctly
- [ ] Data integrity maintained
- [ ] Transactions roll back on error
- [ ] Pagination works correctly

### C. Performance Testing
- [ ] Response time < baseline
- [ ] TPS ≥ 1000
- [ ] Memory usage acceptable
- [ ] CPU usage acceptable

### D. Load Testing
- [ ] Simulate 1000+ concurrent requests
- [ ] Verify connection pooling works
- [ ] Check timeout handling
- [ ] Monitor resource usage

### E. Security Testing
- [ ] SQL injection prevention verified
- [ ] Parameter binding correct
- [ ] Connection string secure
- [ ] Audit logging functional

## Phase 5: Deployment

### A. Pre-Deployment
- [ ] Backup production database
- [ ] Create migration rollback plan
- [ ] Notify team of deployment
- [ ] Schedule maintenance window if needed

### B. Deployment Steps
- [ ] Deploy new application code
- [ ] Execute stored procedure SQL scripts
- [ ] Verify application starts
- [ ] Run smoke tests
- [ ] Monitor error logs

### C. Post-Deployment
- [ ] Monitor performance metrics
- [ ] Check error logs for issues
- [ ] Verify TPS target met
- [ ] Document any issues found

### D. Rollback Plan
- [ ] Deploy previous version if critical issues
- [ ] Revert database changes if needed
- [ ] Notify stakeholders
- [ ] Document root cause

## Post-Migration

- [ ] **Performance Monitoring**
  - [ ] Set up Application Insights
  - [ ] Monitor response times
  - [ ] Track error rates
  - [ ] Monitor TPS continuously

- [ ] **Documentation**
  - [ ] Document any custom procedures
  - [ ] Create runbooks for common operations
  - [ ] Update troubleshooting guide
  - [ ] Share learnings with team

- [ ] **Knowledge Transfer**
  - [ ] Train team on Dapper patterns
  - [ ] Review code with newer developers
  - [ ] Document best practices
  - [ ] Create examples for future projects

- [ ] **Optimization**
  - [ ] Profile slow queries
  - [ ] Add missing indexes
  - [ ] Optimize stored procedures
  - [ ] Consider result caching

## Success Criteria

- [ ] ✅ All CRUD operations working
- [ ] ✅ No errors in production logs
- [ ] ✅ TPS ≥ 1000 (or team target)
- [ ] ✅ Response time improved vs baseline
- [ ] ✅ Connection pooling effective
- [ ] ✅ Unit test coverage ≥ 80%
- [ ] ✅ Integration tests passing
- [ ] ✅ Load tests successful
- [ ] ✅ Team trained on Dapper
- [ ] ✅ Documentation complete

## Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Setup | 1-2 days | NuGet access |
| Phase 2: Migration | 5-15 days | Per entity count |
| Phase 3: Cleanup | 2-3 days | Phase 2 complete |
| Phase 4: Testing | 3-5 days | Phase 3 complete |
| Phase 5: Deployment | 1 day | All tests passing |
| **Total** | **2-4 weeks** | Team size dependent |

## Common Pitfalls to Avoid

❌ **Don't:** Write dynamic SQL queries  
✅ **Do:** Use stored procedures only

❌ **Don't:** Skip index creation  
✅ **Do:** Create indexes on filter columns

❌ **Don't:** Ignore CancellationToken  
✅ **Do:** Pass it through all layers

❌ **Don't:** Skip error handling  
✅ **Do:** Log all errors properly

❌ **Don't:** Test only happy path  
✅ **Do:** Test error scenarios too

❌ **Don't:** Deploy without load testing  
✅ **Do:** Verify TPS target before production

❌ **Don't:** Forget connection pooling  
✅ **Do:** Configure min/max pool size

## Resources

- [DAPPER_GUIDE.md](DAPPER_GUIDE.md) - Comprehensive guide
- [DAPPER_IMPLEMENTATION.md](DAPPER_IMPLEMENTATION.md) - Implementation details
- [DAPPER_QUICK_REFERENCE.md](DAPPER_QUICK_REFERENCE.md) - Quick reference
- [Dapper GitHub](https://github.com/DapperLib/Dapper)
- [SQL Server Stored Procedures](https://docs.microsoft.com/sql/t-sql/statements/create-procedure-transact-sql)

---

**Total Checklist Items:** 100+  
**Estimated Completion Time:** 2-4 weeks  
**Success Rate:** 95%+ with proper planning
