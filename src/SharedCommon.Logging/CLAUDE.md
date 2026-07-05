# SharedCommon.Logging

Structured logging abstraction layer.
Supports: Serilog, ElasticSearch, file, console, database sinks.

## API Surface

- `ILoggerFactory` registration via DI (`AddSharedLogging()`)
- `LogContext` for structured properties
- `CorrelationIdEnricher` for request tracing
- Configuration binding via `IOptions<LoggingOptions>`

## Rules

**Must:**
- Use structured logging (message templates, not string interpolation)
- Include CorrelationId in all logs
- Never log secrets (PII, tokens, API keys)
- Support all sinks via appsettings.json configuration
- Use log levels correctly (Debug/Info/Warning/Error/Critical)

**Forbidden:**
- `Console.WriteLine` anywhere
- `Serilog.Log.*` static methods outside of startup bootstrap
- Hardcoded sink URLs
- Sensitive payload logging
- String concatenation in log messages

## Configuration

```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "Sinks": {
      "Console": { "Enabled": true, "Format": "json" },
      "File": { "Enabled": true, "Path": "logs/app.log", "RollingInterval": "Day" },
      "ElasticSearch": { "Enabled": false, "Url": "" }
    }
  }
}
```

## Design Decisions

See: docs/adr/ADR-002-serilog-standard.md

## Test Strategy

- `TestLogger<T>` fixture for capturing log entries in tests
- Test enrichers produce expected structured properties
- Integration tests verify sink configuration binding

## Extension Points

- Custom enrichers via `ILogEnricher`
- Custom formatters via `ITextFormatter`
- Custom destructuring policies via `IDestructuringPolicy`
