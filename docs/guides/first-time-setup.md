# First-Time Developer Setup

Welcome to SharedCommon. This guide gets you from zero to running tests in under 30 minutes.

## Step 1: Install Prerequisites

```powershell
# Verify .NET 8
dotnet --version   # should be 8.x.x

# Install .NET 8 if needed
winget install Microsoft.DotNet.SDK.8

# Verify Docker
docker --version
```

## Step 2: Clone and Bootstrap

```powershell
git clone <repo-url> Cerberus
cd Cerberus
.\tools\scripts\bootstrap-dev-env.ps1
```

## Step 3: Start Infrastructure

```powershell
docker-compose -f infra/docker/docker-compose.yml up -d
```

Verify services:
- Redis: `redis-cli -h localhost ping` → PONG
- Jaeger UI: http://localhost:16686
- Prometheus: http://localhost:9090

## Step 4: Build and Test

```powershell
dotnet build
dotnet test
```

All tests should be green. If they're not, check:
1. Are Docker services running?
2. Are user secrets set? (see below)
3. Check the error message against docs/runbooks/local-development.md

## Step 5: Set Local Secrets

```powershell
cd src/SharedCommon.Caching
dotnet user-secrets set "Caching:RedisConnectionString" "localhost:6379"
```

## Step 6: Understand the Structure

Read in this order (15 minutes total):
1. [CLAUDE.md](../../CLAUDE.md) — rules and principles
2. [docs/architecture/overview.md](../architecture/overview.md) — system design
3. [.claude/navigation.md](../../.claude/navigation.md) — how to use Claude effectively

## Step 7: Make Your First Change

Use the prompt in `.claude/prompts/add-new-package.md` to add a new package and see the workflow in action.

## Getting Help

- Architecture question → `docs/architecture/`
- Design decision → `docs/adr/`
- Coding question → `docs/standards/`
- Claude workflow → `.claude/skills/`
- Running into a problem → `docs/runbooks/`
