# Package Design Skill

Design robust, extensible NuGet packages.

## When to Use This Skill

Triggers:
- Creating a new package
- Defining the public API surface
- Setting up dependency structure
- Designing extension points

Ask Claude explicitly: "Use package-design skill"

## Input (What You Provide)

- Package name and purpose
- Intended consumers (services, APIs, etc.)
- Key dependencies (if known)

## Output (What You Get)

1. Package folder structure (ready to code)
2. CLAUDE.md (for this package)
3. IServiceCollection extension skeleton
4. Unit test skeleton
5. README.md with usage examples

## Checklist

**Structure:**
- [ ] Single responsibility enforced
- [ ] Folder structure follows convention (src/, tests/)
- [ ] Public APIs separate from internals
- [ ] Extension methods in separate file

**Dependencies:**
- [ ] No circular dependencies
- [ ] No transitive bloat
- [ ] Major version pinned
- [ ] Optional dependencies marked

**API Design:**
- [ ] All public methods XML documented
- [ ] Interfaces for abstraction
- [ ] IServiceCollection extension provided
- [ ] IOptions<T> for configuration
- [ ] Result<T> for error handling

**Observability:**
- [ ] ILogger injected in all services
- [ ] Key operations logged
- [ ] Correlation ID propagation
- [ ] Performance-critical paths marked
- [ ] Health check included (if applicable)

**Testing:**
- [ ] Unit tests for public API
- [ ] Integration tests for DI setup
- [ ] Edge cases covered
- [ ] Mock implementations provided

**Security:**
- [ ] No secrets in code
- [ ] Input validation
- [ ] Secure defaults
- [ ] Documentation of security implications

**Maturity:**
- [ ] README with examples
- [ ] CLAUDE.md for future contributors
- [ ] Semantic versioning planned
- [ ] Breaking change policy documented

## Example Structure

```
SharedCommon.Cache/
├── CLAUDE.md
├── README.md
├── src/
│   ├── CacheOptions.cs
│   ├── ICacheService.cs
│   ├── DistributedCacheService.cs
│   ├── InMemoryCacheService.cs
│   └── ServiceCollectionExtensions.cs
├── tests/
│   ├── CacheServiceTests.cs
│   ├── CacheOptionsTests.cs
│   └── Fixtures/
└── .csproj
```

## Common Mistakes

❌ Fat interface: putting all operations on a single interface
- Why: Breaks ISP, creates forced dependencies
- Fix: Split into focused interfaces (IReadCache, IWriteCache)

❌ Leaking infrastructure types into the public API
- Why: Consumers become coupled to implementation details
- Fix: Use abstraction types only in public signatures

❌ Missing CancellationToken on async methods
- Why: Callers cannot cancel long-running operations
- Fix: Add `CancellationToken ct = default` to every async method

## References

See: docs/standards/package-design.md
See: docs/architecture/package-boundaries.md
See: tools/templates/package-template/
