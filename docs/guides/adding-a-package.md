# Adding a New Package

Step-by-step guide for creating a new SharedCommon package.

## Step 1: Verify Need

Before creating:
- Does an existing package already cover this?
- Can this be an extension to an existing package?
- Does it have a single, clear responsibility?

If yes to any of the first two, talk to the team before proceeding.

## Step 2: Use the Claude Prompt

Open `.claude/prompts/add-new-package.md` and fill in:
- Package name
- Purpose (one sentence)
- Consumers
- Key dependencies

Run it with Claude Code to generate the scaffold.

## Step 3: Review the Scaffold

Claude will generate:
```
src/SharedCommon.XYZ/
├── CLAUDE.md
├── README.md
├── src/
│   ├── IXyzService.cs
│   ├── XyzService.cs
│   ├── XyzOptions.cs
│   └── ServiceCollectionExtensions.cs
└── SharedCommon.XYZ.csproj
```

Verify it passes the package-design checklist in `.claude/skills/package-design/SKILL.md`.

## Step 4: Validate Dependencies

```powershell
# Check dependency rules
.\.claude\hooks\validate-dependencies.ps1
```

Ensure the new package is listed in `docs/architecture/dependency-rules.md`.

## Step 5: Write Tests

Add to `tests/SharedCommon.UnitTests/[PackageName]/`.

Run:
```powershell
dotnet test tests/SharedCommon.UnitTests
```

## Step 6: Update Architecture Tests

Add the new package to `tests/SharedCommon.ArchitectureTests/DependencyTests.cs` with its allowed dependencies.

## Step 7: Update Root Files

- Add to `Directory.Build.props` if any custom build settings needed
- Add to `docs/architecture/package-boundaries.md` with its scope
- Add to `docs/architecture/dependency-rules.md` matrix

## Step 8: PR

Use `.claude/skills/code-review/SKILL.md` checklist before submitting.
