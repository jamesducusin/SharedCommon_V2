# SharedCommon.Observability

OpenTelemetry tracing and metrics for ASP.NET Core services. Exports to any OTLP-compatible backend (Jaeger, Tempo, Honeycomb, Datadog, etc.).

Provides: distributed tracing, metrics, W3C TraceContext propagation, and `x-correlation-id` header propagation across all outgoing calls.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Observability
```

## Registration

```csharp
builder.Services.AddSharedObservability(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Observability": {
      "ServiceName": "OrderService",
      "ServiceVersion": "1.0.0",
      "OtlpEndpoint": "http://otel-collector:4317",
      "SamplingRatio": 1.0,
      "InstrumentAspNetCore": true,
      "InstrumentHttpClient": true
    }
  }
}
```

| Property | Required | Default | Notes |
|----------|----------|---------|-------|
| `ServiceName` | Yes | — | Appears as `service.name` in all traces and metrics. |
| `ServiceVersion` | No | `1.0.0` | Appears as `service.version`. |
| `OtlpEndpoint` | No | `null` | Export disabled when null. gRPC OTLP endpoint (port 4317). |
| `SamplingRatio` | No | `1.0` | `1.0` = 100%, `0.1` = 10%. Errors are always sampled. |
| `InstrumentAspNetCore` | No | `true` | Auto-instrument incoming HTTP requests. |
| `InstrumentHttpClient` | No | `true` | Auto-instrument outgoing `HttpClient` calls. |

## Usage

### Creating custom spans

```csharp
using System.Diagnostics;

public class OrderService
{
    // Use the SharedCommon activity source for your domain
    private static readonly ActivitySource _source =
        new ActivitySource("SharedCommon.OrderService");

    public async Task<Result<Order>> ProcessAsync(Guid orderId, CancellationToken ct)
    {
        using var activity = _source.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);

        try
        {
            var order = await _repo.GetAsync(orderId, ct);

            activity?.SetTag("order.status", order.Status);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result<Order>.Ok(order);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Recording metrics

```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private static readonly Meter _meter = new Meter("SharedCommon.OrderService");
    private static readonly Counter<long> _ordersCreated =
        _meter.CreateCounter<long>("orders.created");
    private static readonly Histogram<double> _processingTime =
        _meter.CreateHistogram<double>("orders.processing_ms");

    public async Task CreateAsync(CreateOrderCommand cmd, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        // ... process ...
        _ordersCreated.Add(1, new KeyValuePair<string, object?>("region", cmd.Region));
        _processingTime.Record(sw.ElapsedMilliseconds);
    }
}
```

### Correlation ID propagation

`CorrelationPropagator` is installed globally and automatically:
- Reads `x-correlation-id` from **incoming** HTTP and gRPC headers and stores it in Baggage
- Writes `x-correlation-id` to **outgoing** `HttpClient` calls alongside `traceparent`

No code required — all `HttpClient` instances created through `IHttpClientFactory` propagate automatically.

## Backend Configuration Examples

**Jaeger (local development)**
```json
{ "OtlpEndpoint": "http://localhost:4317" }
```

**Grafana Tempo**
```json
{ "OtlpEndpoint": "http://tempo:4317" }
```

**No export (tests / local with console only)**
```json
{ "OtlpEndpoint": null }
```

## What Gets Registered

| Component | Notes |
|-----------|-------|
| `CorrelationPropagator` | Set as global `TextMapPropagator` via `Sdk.SetDefaultTextMapPropagator`. |
| OpenTelemetry tracing | All SharedCommon `ActivitySource` names pre-registered. |
| OpenTelemetry metrics | All SharedCommon `Meter` names pre-registered. |
| OTLP exporter | Only when `OtlpEndpoint` is non-null. |
