# Package Design Standards

See `.claude/skills/package-design/SKILL.md` for task-level guidance.

## Public API Surface Rules

- Interfaces, not concrete types, in public method signatures
- No internal types leaked via public members
- Extension methods in a separate `ServiceCollectionExtensions.cs` file
- Configuration via `IOptions<TOptions>` — one options class per package
- `Result<T>` for fallible operations, exceptions for programming errors

## IServiceCollection Extension Pattern

Every package must provide:

```csharp
namespace SharedCommon.Caching;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon caching services.
    /// </summary>
    public static IServiceCollection AddSharedCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddSingleton<ICacheService, HybridCacheService>();
        return services;
    }
}
```

## Options Pattern

```csharp
public sealed class CacheOptions
{
    public const string SectionName = "Caching";

    public int DefaultTtlSeconds { get; set; } = 300;
    public string? RedisConnectionString { get; set; }
}
```

## .csproj Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>SharedCommon.Caching</PackageId>
    <Description>Hybrid caching abstraction for SharedCommon platform.</Description>
    <Authors>SharedCommon Team</Authors>
    <PackageTags>caching;redis;hybridcache;sharedcommon</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="/" />
  </ItemGroup>
</Project>
```

## Dependency Policy

- Reference `Microsoft.Extensions.*` abstractions, not implementations
- No `<PackageReference>` to other SharedCommon packages beyond what's listed in dependency-rules.md
- **All NuGet package versions must be centralized** in `Directory.Packages.props`
  - Never hardcode versions in `.csproj` files (use `<PackageReference Include="Serilog" />` without Version attribute)
  - When adding a new package dependency, add the version to `Directory.Packages.props` first
  - When adding a transitive dependency (e.g., package X needs Microsoft.AspNetCore.Http), add it to central management
  - This ensures consistent versions across all 22+ packages and prevents `NETSDK1064` restore errors

## Versioning

Follow ADR-001. Use `Directory.Packages.props` for version centralization.
