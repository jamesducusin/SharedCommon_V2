# Dependency Rules

## Allowed Dependency Matrix

| Package | May depend on |
|---------|--------------|
| Core | (nothing) |
| Logging | Core |
| Observability | Core, Logging |
| Security | Core, Logging |
| Auth | Core, Logging, Security |
| Validation | Core, Logging |
| Caching | Core, Logging |
| Messaging | Core, Logging, Observability |
| HealthChecks | Core, Logging |
| Middlewares | Core, Logging, ResponseBuilder |
| Utilities | Core |
| Grpc | Core, Logging, Observability, ResponseBuilder |
| GraphQL | Core, Logging, Observability, ResponseBuilder |
| Cloud | Core, Logging, Observability |
| Resiliency | Core, Logging |
| ResponseBuilder | Core |

## Forbidden Patterns

- Any package referencing a package that references it back (circular)
- Core referencing any other SharedCommon package
- Caching referencing Auth
- Auth referencing Caching directly (use IDistributedCache abstraction)

## Adding a New Dependency

Before adding a new `<ProjectReference>` or `<PackageReference>`:

1. Check this table — is it allowed?
2. If not listed — update this file and the architecture tests in the same PR
3. Run `.claude/hooks/validate-dependencies.ps1` locally
4. Ensure architecture tests still pass

## Third-Party Package Policy

- Pin to major version in `Directory.Packages.props`
- Transitive dependencies are visible but not controlled directly
- Run `dotnet list package --vulnerable` before each release
- Prefer Microsoft.Extensions.* abstractions over third-party where possible
