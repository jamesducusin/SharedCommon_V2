# ADR-001: Package Versioning Strategy

**Status:** Accepted
**Date:** 2026-01-01

## Context

SharedCommon packages are consumed by multiple downstream services. Breaking changes without a clear versioning strategy cause cascading failures. We need a consistent policy that communicates intent and protects consumers.

## Decision

Use **Semantic Versioning 2.0** (semver.org) for all packages:

- **MAJOR** version: breaking public API change
- **MINOR** version: new backward-compatible feature
- **PATCH** version: backward-compatible bug fix

All packages in the solution share the same major version (lock-step MAJOR). Minor and patch versions may differ per package.

### Breaking Change Process

1. Mark old API with `[Obsolete("Use XYZ instead. Will be removed in v{N+1}.0")]`
2. Keep the old API for at least one MINOR release
3. Document migration path in CHANGELOG.md
4. Remove in next MAJOR

### Pre-release

Use `-preview.{n}` suffix for preview packages during development cycles.

## Consequences

- Consumers can confidently upgrade MINOR versions without fear of breakage
- MAJOR version bumps are explicit signals requiring consumer action
- Lock-step MAJOR simplifies version compatibility matrix across packages
