# Publishing Packages

## Pre-Publish Checklist

- [ ] All tests pass: `dotnet test`
- [ ] Architecture tests pass: `dotnet test tests/SharedCommon.ArchitectureTests`
- [ ] No known vulnerabilities: `dotnet list package --vulnerable`
- [ ] Version bumped in `Directory.Build.props` per ADR-001
- [ ] CHANGELOG.md updated with release notes
- [ ] Breaking changes documented with migration path
- [ ] Code review approved

## Manual Publish

```powershell
# Pack all packages
dotnet pack -c Release

# Publish to NuGet.org
.\tools\scripts\publish-all.ps1 -ApiKey $env:NUGET_API_KEY

# Publish to private feed
.\tools\scripts\publish-all.ps1 -Source "https://pkgs.dev.azure.com/.../nuget/v3/index.json" -ApiKey $env:AZURE_DEVOPS_TOKEN
```

## Automated Publish (CI/CD)

Triggered by pushing a version tag:
```bash
git tag v2.1.0
git push origin v2.1.0
```

GitHub Actions workflow builds, tests, and publishes all packages.

## Version Bump Process

1. Update `<VersionPrefix>` in `Directory.Build.props`
2. Update CHANGELOG.md
3. Commit: `git commit -m "chore: bump version to 2.1.0"`
4. Tag: `git tag v2.1.0`
5. Push tag to trigger CI/CD

## Rollback

If a bad version is published:
1. Unlist (not delete) on NuGet: `dotnet nuget delete SharedCommon.X 2.1.0 --non-interactive`
2. Publish patch version with fix
3. Document incident in CHANGELOG.md

See: docs/runbooks/rollback-procedures.md
