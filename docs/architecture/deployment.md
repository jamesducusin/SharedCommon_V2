# Deployment Strategy

## NuGet Package Delivery

SharedCommon packages are distributed as NuGet packages, not deployed as services.

### Publishing Pipeline

1. PR merged to `main`
2. GitHub Actions builds and tests
3. If version tag pushed (`v*.*.*`), packages are published to NuGet
4. Semantic version bumps follow ADR-001

### Version Management

- `Directory.Packages.props` — all third-party versions centralized
- `Directory.Build.props` — shared MSBuild properties for all packages
- Per-package `.csproj` — only package-specific overrides

## Container Support

Sample applications and reference implementations include Docker support.

### Dockerfile Convention

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sample.Api.dll"]
```

## Kubernetes Patterns

Reference Kubernetes manifests in `infra/kubernetes/` demonstrate:
- Resource limits and requests
- Liveness and readiness probes (via SharedCommon.HealthChecks)
- ConfigMap for non-secret configuration
- Secret references for credentials (never hardcoded)
- Horizontal pod autoscaling configuration

## Observability Infrastructure

See `infra/observability/` for:
- Prometheus scrape configuration
- Grafana dashboards for SharedCommon metrics
- Jaeger/OpenTelemetry collector configuration

See: infra/README.md
