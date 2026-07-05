# Local Development Setup

## Prerequisites

- .NET 8 SDK (`dotnet --version` should show 8.x)
- Docker Desktop (for Redis, Kafka, Jaeger in development)
- IDE: Visual Studio 2022 17.8+ or Rider 2023.3+

## First-Time Setup

```powershell
# Clone and bootstrap
git clone <repo-url>
cd Cerberus
.\tools\scripts\bootstrap-dev-env.ps1
```

The bootstrap script:
1. Verifies .NET SDK version
2. Starts Docker services (`docker-compose up -d`)
3. Restores NuGet packages
4. Runs architecture tests to verify setup

## Running Services

```powershell
# Build everything
dotnet build

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/SharedCommon.UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Format code
dotnet format
```

## Docker Services

```powershell
# Start all infrastructure
docker-compose -f infra/docker/docker-compose.yml up -d

# Services started:
# Redis        → localhost:6379
# Kafka        → localhost:9092
# Jaeger UI    → http://localhost:16686
# Prometheus   → http://localhost:9090
```

## Secrets Configuration

```powershell
# Set local secrets (never commit these)
cd src/SharedCommon.Caching
dotnet user-secrets set "Caching:RedisConnectionString" "localhost:6379"
```

## Debugging Architecture Tests

```powershell
dotnet test tests/SharedCommon.ArchitectureTests -v detailed
```

See: guides/first-time-setup.md for full onboarding guide.
