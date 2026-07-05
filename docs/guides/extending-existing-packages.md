# Extending Existing Packages

## When to Extend vs Create New

**Extend an existing package when:**
- Adding a new implementation of an existing interface (e.g., new cache backend)
- Adding a configuration option to an existing feature
- Adding a new sink to SharedCommon.Logging

**Create a new package when:**
- The concern is orthogonal to the existing package's responsibility
- Adding it would bloat the package with unrelated dependencies
- Consumers shouldn't have to take the new functionality to use the existing package

## Safe Extension Points

Every package defines explicit extension points. Look in the package's `CLAUDE.md`:
- Custom enrichers (Logging)
- Custom sinks (Logging)
- Custom cache implementations (Caching)
- Custom validators (Validation)
- Custom middleware (Middlewares)
- Custom health checks (HealthChecks)

## How to Add a New Implementation

Example: Adding a MongoDB cache backend to SharedCommon.Caching

```
1. Create a new class implementing ICacheService
2. Add optional NuGet dependency for MongoDB driver
3. Add AddMongoDbCache() extension method
4. Unit test the new implementation
5. Update SharedCommon.Caching/CLAUDE.md with the new implementation
6. Update README.md with usage example
```

## How to Add a Configuration Option

```
1. Add property to the relevant XxxOptions class
2. Add default value (secure, safe defaults)
3. Update README.md with the new option
4. Add unit test for the new behavior
5. If it's a breaking change → follow ADR-001 deprecation process
```

## Deprecating Existing API

```csharp
// Step 1: Mark obsolete with replacement info
[Obsolete("Use AddSharedCachingV2 instead. Will be removed in v3.0. See migration guide in README.")]
public static IServiceCollection AddSharedCaching(this IServiceCollection services) { ... }

// Step 2: Add new API
public static IServiceCollection AddSharedCachingV2(this IServiceCollection services, ...) { ... }
```

Keep the old API for at least one MINOR release before removing in the next MAJOR.

## Contribution Checklist

Before submitting a PR for an extension:
- [ ] Package's CLAUDE.md reviewed and followed
- [ ] `.claude/skills/code-review/SKILL.md` checklist completed
- [ ] Tests added for new functionality
- [ ] Architecture tests still pass
- [ ] No new transitive dependencies unless approved
- [ ] README.md updated
