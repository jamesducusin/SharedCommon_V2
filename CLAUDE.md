# SharedCommon Platform

Enterprise-grade .NET infrastructure platform.
Modular, observable, secure, cloud-native.

## Core Principles

- Single responsibility per package
- Independent installability
- Configuration-driven behavior
- Secure by default
- Observable by default
- Zero hardcoded magic

## Architecture Rules

**Must follow:**
- No circular dependencies
- No infrastructure leakage into abstractions
- All public APIs require XML docs
- All code requires unit tests
- Structured logging everywhere
- Dependency injection always
- Nullable ref types enabled
- Async/await + CancellationToken

**Forbidden:**
- Hardcoded secrets, ports, URLs
- Static mutable state
- Service locator pattern
- Silent exception swallowing
- Business logic in middleware
- God classes, fat controllers
- Magic strings, TODO comments
- Copy-paste implementations

## When Uncertain

1. Check docs/adr/ for design decisions
2. Use .claude/skills/ for task guidance
3. Review relevant package CLAUDE.md
4. Ask Claude explicitly

## Repository Layout

- src/ = packages | tests/ = all tests | docs/ = decisions
- .claude/ = AI workflows | infra/ = deployment

## Quick Commands

```
dotnet build               # Build all
dotnet test               # Run all tests
dotnet format             # Auto-format
dotnet pack               # Create NuGet packages
```

See docs/ for detailed standards.
Use .claude/skills/ for implementation guidance.
