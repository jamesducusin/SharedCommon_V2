# Observability Skill

Wire up structured logging, distributed tracing, and metrics.

## When to Use This Skill

Triggers:
- Setting up structured logging in a new package
- Adding correlation ID propagation
- Instrumenting metrics
- Setting up distributed tracing with OpenTelemetry
- Debugging missing observability in production

Ask Claude explicitly: "Use observability skill"

## Input (What You Provide)

- Module or package name
- Observability gap or requirement

## Output (What You Get)

- Logging patterns with correct levels and structure
- Trace instrumentation setup
- Metrics instrumentation setup

## Checklist

**Logging:**
- [ ] ILogger<T> injected via DI (never static)
- [ ] Structured properties, not string interpolation
- [ ] CorrelationId included on every log entry
- [ ] Log levels used correctly (Debug/Info/Warning/Error/Critical)
- [ ] No PII in log messages
- [ ] Exceptions logged with `ex` parameter, not `.ToString()`

**Tracing:**
- [ ] Activity source registered for the package
- [ ] Spans created for meaningful operations
- [ ] Span attributes include operation context
- [ ] Parent span propagated across async calls
- [ ] Error spans marked with Status.Error

**Metrics:**
- [ ] Counters for operation counts
- [ ] Histograms for latency
- [ ] Gauges for resource levels
- [ ] Meter registered at startup
- [ ] Metric names follow `package.operation.unit` convention

**Correlation:**
- [ ] CorrelationId extracted from incoming requests
- [ ] CorrelationId propagated to outgoing requests
- [ ] CorrelationId available in all log scopes
- [ ] Same CorrelationId in traces and logs

## Correct Logging Patterns

```csharp
// Good: structured
_logger.LogInformation("Order {OrderId} processed in {ElapsedMs}ms", orderId, elapsed);

// Bad: interpolated
_logger.LogInformation($"Order {orderId} processed in {elapsed}ms");

// Good: exception
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);

// Bad: exception as string
_logger.LogError("Failed: " + ex.ToString());
```

## References

See: docs/architecture/observability-strategy.md
See: docs/standards/logging-guidelines.md
See: docs/adr/ADR-002-serilog-standard.md
See: docs/adr/ADR-004-opentelemetry.md
See: src/SharedCommon.Observability/CLAUDE.md
