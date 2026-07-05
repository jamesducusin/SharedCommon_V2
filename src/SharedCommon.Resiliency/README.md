# SharedCommon.Resiliency

Polly v8 resilience pipelines: exponential back-off retry with jitter, circuit breaker with state-transition logging, and timeout — pre-configured and named for reuse.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Resiliency
```

## Registration

```csharp
builder.Services.AddSharedResiliency(builder.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "Resiliency": {
      "Retry": {
        "MaxAttempts": 3,
        "BaseDelay": "00:00:00.500",
        "MaxDelay": "00:00:30"
      },
      "CircuitBreaker": {
        "FailureRatio": 0.5,
        "MinimumThroughput": 5,
        "SamplingDuration": "00:01:00",
        "BreakDuration": "00:00:30"
      },
      "Timeout": {
        "Duration": "00:00:30"
      }
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `Retry.MaxAttempts` | `3` | Includes the original attempt. Range: 1–10. |
| `Retry.BaseDelay` | `500ms` | Starting back-off. Grows exponentially with jitter. |
| `Retry.MaxDelay` | `30s` | Back-off is capped here. |
| `CircuitBreaker.FailureRatio` | `0.5` | 50% failures opens the circuit. Range: 0–1. |
| `CircuitBreaker.MinimumThroughput` | `5` | Minimum requests before ratio is evaluated. |
| `CircuitBreaker.SamplingDuration` | `60s` | Failure ratio measurement window. |
| `CircuitBreaker.BreakDuration` | `30s` | Time the circuit stays open. |
| `Timeout.Duration` | `30s` | Maximum wall-clock time for any operation. |

## Pre-built Pipelines

Three named pipelines are registered automatically:

| Name | Strategy |
|------|----------|
| `"default"` | Retry → Circuit Breaker → Timeout |
| `"retry"` | Retry only |
| `"timeout"` | Timeout only |

## Usage

### Wrapping any async operation

Inject `IResiliencyPolicyProvider` and execute through a named pipeline:

```csharp
public class PaymentService(IResiliencyPolicyProvider policies)
{
    public async Task<Result<PaymentResult>> ChargeAsync(
        PaymentRequest request,
        CancellationToken ct)
    {
        var pipeline = policies.GetPipeline(ResiliencyPolicyProvider.Default);

        return await pipeline.ExecuteAsync(async token =>
        {
            var response = await _httpClient.PostAsJsonAsync("/charge", request, token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaymentResult>(token);
            return Result<PaymentResult>.Ok(result!);

        }, ct);
    }
}
```

### Typed pipeline (Result&lt;T&gt;)

```csharp
var pipeline = policies.GetPipeline<Result<Order>>(ResiliencyPolicyProvider.RetryOnly);

var result = await pipeline.ExecuteAsync(async ct =>
    await _orderRepo.GetAsync(id, ct), ct);
```

### Applying to HttpClient

```csharp
builder.Services
    .AddHttpClient<IPaymentClient, PaymentClient>(client =>
    {
        client.BaseAddress = new Uri("https://payment.internal");
    })
    .AddSharedResilienceHandler();   // applies "default" pipeline to all calls
```

The handler reads `ResiliencyOptions` from DI — no extra configuration needed.

### Circuit breaker state transitions

The circuit breaker logs state changes automatically (no code required):

```
[Warning] Circuit breaker opened. Break duration: 00:00:30
[Info] Circuit breaker half-open. Testing next request.
[Info] Circuit breaker closed.
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IResiliencyPolicyProvider` | Singleton | Wraps `ResiliencePipelineRegistry<string>`. |
| `ResiliencePipelineRegistry<string>` | Singleton | Pre-populated with `"default"`, `"retry"`, `"timeout"`. |
