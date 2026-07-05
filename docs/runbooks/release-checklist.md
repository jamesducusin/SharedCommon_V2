# Release Checklist

## Code Quality

- [ ] All unit tests pass (`dotnet test tests/SharedCommon.UnitTests`)
- [ ] All integration tests pass (`dotnet test tests/SharedCommon.IntegrationTests`)
- [ ] All architecture tests pass (`dotnet test tests/SharedCommon.ArchitectureTests`)
- [ ] No compiler warnings (`dotnet build /p:TreatWarningsAsErrors=true`)
- [ ] Code formatted (`dotnet format --verify-no-changes`)

## Security

- [ ] No hardcoded secrets in diff (`git diff main...HEAD | grep -i "password\|secret\|apikey"`)
- [ ] Dependency vulnerability scan clean (`dotnet list package --vulnerable`)
- [ ] Security tests pass (`dotnet test tests/SharedCommon.SecurityTests`)
- [ ] New endpoints have authorization checks

## Documentation

- [ ] CHANGELOG.md updated with all changes since last release
- [ ] Breaking changes documented with migration guide
- [ ] New public APIs have XML documentation
- [ ] Affected package README.md updated

## Versioning

- [ ] Version follows semver (ADR-001)
- [ ] MAJOR bumped for breaking changes
- [ ] MINOR bumped for new features
- [ ] PATCH bumped for bug fixes only
- [ ] Pre-release suffix removed (`-preview.N`)

## Performance

- [ ] No obvious regressions in performance tests
- [ ] New hot-path code has allocation analysis

## Final

- [ ] PR approved by at least one reviewer
- [ ] All CI checks green
- [ ] Version tag ready to push
