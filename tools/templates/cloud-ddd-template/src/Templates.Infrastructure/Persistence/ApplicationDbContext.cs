namespace Templates.Infrastructure.Persistence;

/// <summary>
/// ⚠️ DEPRECATED: This file can be safely deleted.
/// 
/// The template has been refactored to use Dapper with stored procedures
/// instead of Entity Framework Core. All data access is now handled through:
/// 
/// - IDapperRepository<T, TId> - Generic repository for all entities
/// - IStoredProcedureExecutor - Convenience wrapper for custom procedures
/// - DapperServiceCollectionExtensions - Dependency injection setup
/// 
/// See: docs/guides/DAPPER_GUIDE.md for migration information.
/// </summary>
[Obsolete("Use Dapper repositories instead. See DAPPER_GUIDE.md for details.", error: true)]
public sealed class ApplicationDbContext
{
    private ApplicationDbContext() { }
}
