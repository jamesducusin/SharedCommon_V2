# Prompt: Add New Package

Use this prompt when creating a new SharedCommon package.

---

## Prompt

```
I need to add a new package to the SharedCommon platform.

Package name: [SharedCommon.XYZ]
Purpose: [What this package does in one sentence]
Consumers: [Who will use this package]
Key dependencies: [Any known dependencies]

Use the package-design skill (.claude/skills/package-design/SKILL.md) to:
1. Generate the folder structure
2. Create CLAUDE.md for this package
3. Create the IServiceCollection extension skeleton
4. Create the unit test skeleton
5. Create README.md with usage examples

Follow all rules in the root CLAUDE.md and the package-design checklist.
```

---

## Expected Output

- `src/SharedCommon.XYZ/CLAUDE.md`
- `src/SharedCommon.XYZ/README.md`
- `src/SharedCommon.XYZ/src/IXyzService.cs`
- `src/SharedCommon.XYZ/src/XyzService.cs`
- `src/SharedCommon.XYZ/src/XyzOptions.cs`
- `src/SharedCommon.XYZ/src/ServiceCollectionExtensions.cs`
- `src/SharedCommon.XYZ/tests/XyzServiceTests.cs`
- `src/SharedCommon.XYZ/SharedCommon.XYZ.csproj`
