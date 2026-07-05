# Prompt: Create Service

Use this prompt when implementing a service within an existing package.

---

## Prompt

```
I need to implement a new service in [SharedCommon.XYZ].

Service name: [XyzService]
Responsibility: [What this service does]
Dependencies: [ILogger, IOptions<T>, other interfaces]

Read src/SharedCommon.XYZ/CLAUDE.md before writing any code.

Requirements:
- Follow Clean Architecture (use clean-architecture skill if uncertain)
- Inject ILogger<XyzService>
- Accept CancellationToken on all async methods
- Use Result<T> for error paths, not exceptions
- Full XML docs on the public interface
- Unit tests in tests/XyzServiceTests.cs
```

---

## Expected Output

- `src/IXyzService.cs` — interface with XML docs
- `src/XyzService.cs` — implementation with logging
- `tests/XyzServiceTests.cs` — unit tests (happy + error paths)
- Updated `ServiceCollectionExtensions.cs`
