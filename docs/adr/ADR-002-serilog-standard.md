# ADR-002: Serilog as Structured Logging Standard

**Status:** Accepted
**Date:** 2026-01-01

## Context

Consistent structured logging is critical for production observability. We need a logging library that supports structured properties, multiple sinks, and integrates with .NET's `ILogger<T>` abstraction so consumers are not coupled to the implementation.

## Decision

Use **Serilog** as the backing implementation for `Microsoft.Extensions.Logging`.

- All code uses `ILogger<T>` from `Microsoft.Extensions.Logging` (not Serilog directly)
- SharedCommon.Logging configures Serilog sinks via `appsettings.json`
- Default sinks: Console (structured JSON), File (rolling)
- Optional sinks: ElasticSearch, Application Insights (via configuration)
- Enrichers always enabled: CorrelationId, MachineName, Environment, ThreadId

### Forbidden

- `Serilog.Log.*` static methods anywhere outside startup configuration
- `Console.Write*` in any library or service code
- String interpolation in log messages (use message templates)

## Consequences

- Consumers depend only on `Microsoft.Extensions.Logging` abstractions
- Switching the logging backend requires changes only in SharedCommon.Logging
- Structured log properties enable precise querying in ElasticSearch/Seq/Loki
