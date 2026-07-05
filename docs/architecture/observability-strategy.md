# Observability Strategy

## Three Pillars

### 1. Structured Logging (Serilog)

- All logs written via `ILogger<T>` (never `Console.Write*` or static `Log.*`)
- Log messages use message templates, not string interpolation
- CorrelationId enriched on every log entry
- Sensitive data (PII, tokens) never logged
- Levels: Debug (dev only), Information (normal ops), Warning (recoverable), Error (needs attention), Critical (service down)

### 2. Distributed Tracing (OpenTelemetry)

- Each package registers an `ActivitySource` with its name
- Span created for every external call (HTTP, Redis, DB, Kafka)
- Span attributes include: operation name, entity IDs, user context (non-PII)
- Errors mark span with `Status.Error` and record the exception
- W3C trace context propagated across all service boundaries

### 3. Metrics (OpenTelemetry Metrics / Prometheus)

- `Meter` registered per package
- Counters for: request count, error count, cache hits/misses
- Histograms for: request duration, external call duration
- Gauges for: queue depth, connection pool size
- Metric names follow: `{package_name}.{operation}.{unit}`

## Correlation ID Flow

```
Inbound HTTP/gRPC request
  → CorrelationIdMiddleware extracts or generates ID
  → Stored in HttpContext.Items["CorrelationId"]
  → Pushed to Serilog LogContext
  → Propagated via Activity.Current baggage
  → Injected into outgoing HTTP headers (X-Correlation-ID)
  → Injected into Kafka message headers
```

## Package Requirements

Every package with services must:
1. Accept `ILogger<T>` in constructor
2. Log operation entry at Debug level
3. Log success/failure at Info/Error level
4. Register ActivitySource if making external calls
5. Register Meter if tracking operation counts or durations

See: docs/adr/ADR-002-serilog-standard.md
See: docs/adr/ADR-004-opentelemetry.md
