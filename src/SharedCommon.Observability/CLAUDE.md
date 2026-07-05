# SharedCommon.Observability

OpenTelemetry tracing and metrics instrumentation.
Provides ActivitySource, Meter, and correlation propagation.

## API Surface

- `AddSharedObservability(IConfiguration)` — registers OTel tracing + metrics
- `ObservabilityOptions` — OTLP endpoint, service name, sampling rate
- `ActivitySources` — named sources for each SharedCommon package
- `Meters` — named meters for each SharedCommon package
- `CorrelationPropagator` — W3C TraceContext + Baggage propagation

## Rules

**Must:**
- Register ActivitySource per package with package name
- Create spans for: HTTP calls, Redis ops, Kafka produce/consume, DB queries
- Mark error spans with `Status.Error` and record exception
- Propagate W3C TraceContext in all outgoing calls
- Export to OTLP endpoint configured in `ObservabilityOptions`

**Forbidden:**
- Hardcoded OTLP endpoints
- Sampling rates that hide production errors (always sample errors)
- Span names that include PII
- Creating spans for trivial operations (no-op spans waste resources)

## Design Decisions

See: docs/adr/ADR-004-opentelemetry.md

## Test Strategy

- Unit test span creation with `ActivityListener` in test mode
- Verify correlation ID in span baggage
- Integration tests verify traces export to Jaeger (docker-compose)

## Extension Points

- Custom `ISampler` for adaptive sampling
- Custom exporters via `AddOtlpExporter` configuration
