# ADR-004: OpenTelemetry for Distributed Tracing and Metrics

**Status:** Accepted
**Date:** 2026-01-01

## Context

Microservices require distributed tracing to understand request flow across service boundaries. Metrics need to be vendor-neutral to avoid lock-in. We need a single standard that covers both concerns.

## Decision

Use **OpenTelemetry** for all tracing and metrics. Serilog (ADR-002) handles logging; OpenTelemetry handles traces and metrics.

### Tracing

- Each package registers a named `ActivitySource`
- Spans created for: external HTTP calls, Redis operations, Kafka produce/consume, database queries
- W3C TraceContext propagated across all async boundaries
- Default exporter: OTLP (configurable endpoint)

### Metrics

- Each package registers a named `Meter`
- Standard instruments: `Counter`, `Histogram`, `ObservableGauge`
- Prometheus endpoint exposed via `/metrics` (configurable)
- Default exporter: Prometheus or OTLP (configurable)

### Correlation Between Signals

- TraceId injected into Serilog log entries as structured property
- Same CorrelationId used in logs and trace baggage
- Enables log-to-trace pivoting in observability UIs

### Collector

- Production: OpenTelemetry Collector (sidecar or daemonset)
- Development: Jaeger all-in-one via docker-compose

## Consequences

- No vendor lock-in for traces/metrics
- Works with Jaeger, Zipkin, Tempo, Datadog, Azure Monitor
- Requires OTLP collector in production environment
