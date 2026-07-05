# SharedCommon.FeatureFlags

Feature flag evaluation for ASP.NET Core services. Built on Microsoft.FeatureManagement — config-driven, context-aware, and A/B-test ready. No external service required.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.FeatureFlags
```

## Registration

```csharp
builder.Services.AddSharedFeatureFlags(builder.Configuration);
```

## Configuration

Feature definitions live under the standard `FeatureManagement` key:

```json
{
  "FeatureManagement": {
    "NewCheckoutFlow": true,
    "DarkMode": false,
    "BetaDashboard": {
      "EnabledFor": [
        {
          "Name": "Percentage",
          "Parameters": { "Value": 20 }
        }
      ]
    },
    "AdminOnlyFeature": {
      "EnabledFor": [
        {
          "Name": "Targeting",
          "Parameters": {
            "Audience": {
              "Groups": [{ "Name": "admin", "RolloutPercentage": 100 }]
            }
          }
        }
      ]
    }
  },
  "SharedCommon": {
    "FeatureFlags": {
      "CacheTtlSeconds": 0,
      "LogEvaluations": true
    }
  }
}
```

### Built-in Filters

| Filter | Description |
|--------|-------------|
| `AlwaysOn` | Always enabled |
| `Percentage` | Enabled for X% of requests |
| `Targeting` | Enabled for specific users or groups |
| `TimeWindow` | Enabled within a time range |

---

## Checking a Flag

Inject `IFeatureFlagService`:

```csharp
public class CheckoutService(IFeatureFlagService features)
{
    public async Task<IActionResult> PlaceOrderAsync(Order order, CancellationToken ct)
    {
        if (await features.IsEnabledAsync("NewCheckoutFlow", ct))
            return await NewFlowAsync(order, ct);

        return await LegacyFlowAsync(order, ct);
    }
}
```

### Contextual Evaluation (Targeting)

```csharp
var ctx = new FeatureFlagContext(UserId: currentUser.Id, Groups: currentUser.Roles.ToList());
if (await features.IsEnabledForAsync("BetaDashboard", ctx, ct))
    return RedirectToAction("BetaDashboard");
```

### Controller Action Filter

```csharp
[FeatureGate("NewCheckoutFlow")]
[HttpPost("checkout")]
public IActionResult NewCheckout([FromBody] CheckoutRequest request) { ... }
```

### Discovering Active Flags

```csharp
var activeFlags = await features.GetEnabledFeaturesAsync(ct);
```

---

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IFeatureFlagService` | Scoped | Wraps `IFeatureManager`. |
| `IFeatureManager` | Scoped | Microsoft.FeatureManagement default. |
| `FeatureFlagOptions` | Singleton (Options) | SharedCommon overrides. Validated at startup. |
