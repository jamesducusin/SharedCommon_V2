# NuGet Packaging Skill

Version, build, and publish NuGet packages.

## When to Use This Skill

Triggers:
- Publishing a new package version
- Setting up versioning for a new package
- Handling breaking changes
- Configuring package metadata

Ask Claude explicitly: "Use nuget-packaging skill"

## Checklist

- [ ] Semantic versioning applied (MAJOR.MINOR.PATCH)
- [ ] Breaking changes increment MAJOR
- [ ] New features increment MINOR
- [ ] Bug fixes increment PATCH
- [ ] `<PackageId>` matches folder name
- [ ] `<Description>` is accurate and meaningful
- [ ] `<Authors>` and `<Company>` set
- [ ] `<PackageTags>` relevant for discoverability
- [ ] `<GenerateDocumentationFile>true</GenerateDocumentationFile>` set
- [ ] `<Nullable>enable</Nullable>` set
- [ ] `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` set
- [ ] README.md included in package (`<PackageReadmeFile>`)
- [ ] CHANGELOG.md updated before publish
- [ ] Architecture tests pass before publish

## Breaking Change Policy

Before incrementing MAJOR:
1. Deprecate with `[Obsolete]` in prior minor release
2. Document migration path in CHANGELOG.md
3. Run consumer impact analysis

## References

See: docs/adr/ADR-001-package-versioning.md
See: docs/runbooks/publishing-packages.md
See: tools/scripts/publish-all.ps1
See: Directory.Packages.props
