# SharedCommon.Logging

Structured logging via Serilog. Replaces the default ASP.NET Core logging provider.

Supports: console, rolling file, Elasticsearch, and database sinks — all configurable via `appsettings.json` with no code changes.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Logging
```

## Registration

```csharp
builder.Services.AddSharedCommonLogging(builder.Configuration);
```

This clears the default logging providers and installs Serilog as the sole provider. Call it before any service that injects `ILogger<T>`.

## Configuration

```json
{
  "SharedCommon": {
    "Logging": {
      "ApplicationName": "OrderService",
      "MinimumLevel": "Information",
      "Console": {
        "Enabled": true,
        "Theme": "Colored"
      },
      "File": {
        "Enabled": false,
        "Path": "./logs/app-.txt",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 30
      },
      "Elasticsearch": {
        "Enabled": false,
        "Url": "http://elasticsearch:9200",
        "IndexFormat": "logs-{0:yyyy.MM.dd}"
      }
    }
  }
}
```

### Key properties

| Property | Default | Notes |
|----------|---------|-------|
| `ApplicationName` | required | Stamped on every log entry. |
| `MinimumLevel` | `Information` | `Debug` \| `Information` \| `Warning` \| `Error` \| `Fatal` |
| `Console.Enabled` | `true` | — |
| `Console.Theme` | `Colored` | `Colored` \| `Grayscale` \| `None` |
| `Serilog.Format` | `Json` | `Json` \| `CompactJson` \| `Text` |
| `File.Enabled` | `false` | Set `true` to enable rolling file sink. |
| `File.Path` | `./logs/app-.txt` | The `-` is where the date is inserted. |
| `File.RollingInterval` | `Day` | `Hour` \| `Day` \| `Month` \| `Year` |
| `Elasticsearch.Enabled` | `false` | Requires a reachable ES cluster. |
| `ExcludePatterns` | `[]` | Message substrings to suppress (e.g. `/health` probes). |

## Usage

Inject `ILogger<T>` exactly as normal — the Serilog provider handles everything.

```csharp
public class OrderService(ILogger<OrderService> logger)
{
    public async Task ProcessAsync(Order order, CancellationToken ct)
    {
        // Always use message templates, never string interpolation
        logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}",
            order.Id, order.CustomerId);

        try
        {
            await DoWorkAsync(order, ct);
            logger.LogInformation("Order {OrderId} processed successfully", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order {OrderId}", order.Id);
            throw;
        }
    }
}
```

### Adding structured context

Use `ILogger.BeginScope` to attach properties to all log entries within a block:

```csharp
using (logger.BeginScope(new Dictionary<string, object>
{
    ["OrderId"]    = order.Id,
    ["CustomerId"] = order.CustomerId
}))
{
    // Every log entry inside this scope carries OrderId and CustomerId
    logger.LogInformation("Validating order");
    logger.LogInformation("Charging payment");
}
```

### Silencing noisy endpoints

```json
{
  "SharedCommon": {
    "Logging": {
      "ExcludePatterns": ["/health", "/metrics", "Request starting"]
    }
  }
}
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| Serilog `ILogger` | Singleton | Replaces all default providers. |
| `CorrelationIdEnricher` | Singleton | Stamps correlation ID on every entry. |
| `LoggingHealthCheck` | — | Registered under key `"logging"`. |
