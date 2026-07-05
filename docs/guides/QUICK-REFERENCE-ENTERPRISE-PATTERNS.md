# Quick Reference - Enterprise Patterns

**One-Minute Integration Guide**

## 1. Rate Limiting

**Setup:**
```csharp
// Program.cs
builder.Services.AddDistributedRateLimiting(builder.Configuration);
app.UseDistributedRateLimiting();  // Add early in middleware
```

**Config:**
```json
{
  "RateLimit": {
    "Enabled": true,
    "Limit": 100,
    "WindowSeconds": 60,
    "StrictMode": true
  }
}
```

**Use:**
```csharp
var result = await rateLimiter.TryAcquireAsync("user-id");
if (!result.Allowed) return Results.StatusCode(429);
```

---

## 2. Feature Flags

**Setup:**
```csharp
builder.Services.AddDistributedFeatureFlags();
```

**Use:**
```csharp
var enabled = await flags.IsEnabledAsync("feature-name", userId);
if (enabled) { /* show new feature */ }
```

**Rollout:**
```csharp
await flags.SetRolloutPercentageAsync("feature", 10);  // 10% of users
await flags.EnableAsync("feature");                    // Full rollout
await flags.DisableAsync("feature");                   // Instant rollback
```

---

## 3. Message Handling

**Setup:**
```csharp
builder.Services.AddPoisonMessageHandling();
```

**Use:**
```csharp
var result = await poisonHandler.ProcessWithRetryAsync(
    messageId: "123",
    messageType: "OrderEvent",
    processAsync: async () => await _service.HandleAsync(order),
    shouldRetry: ex => ex is TimeoutException
);
```

---

## 4. Zero-Downtime Migrations

**Setup:**
```csharp
builder.Services.AddZeroDowntimeMigrations(connectionString);
```

**Use:**
```csharp
// Add column (no lock)
await migrations.AddNullableColumnAsync("Orders", "ProcessedAt", "DATETIME2");

// Backfill in batches
await migrations.BackfillColumnAsync("Orders", "ProcessedAt", 
    "SET ProcessedAt = CreatedAt");

// Create index (no lock)
await migrations.CreateIndexAsync("Orders", "IX_ProcessedAt", 
    new[] { "ProcessedAt" });
```

---

## 5. Configuration Validation

**Setup:**
```csharp
builder.Services.AddEnterpriseConfigurationValidation(builder.Configuration);
var app = builder.Build();
app.Services.ValidateStartupConfiguration();  // Throws if invalid
```

**Config Checklist:**
- ✅ ConnectionStrings:DefaultConnection
- ✅ ConnectionStrings:Redis
- ✅ Jwt:Key (32+ chars)
- ✅ Jwt:Issuer
- ✅ Jwt:Audience
- ✅ Observability:ServiceName
- ✅ Observability:Environment

---

## Common Patterns

### Per-User Rate Limiting
```csharp
var userId = ctx.User?.FindFirst("sub")?.Value ?? "anonymous";
var result = await rateLimiter.TryAcquireAsync(userId);
```

### Canary Deployment
```csharp
// Day 1: 1% of users
await flags.SetRolloutPercentageAsync("new-api", 1);

// Day 3: 10% of users
await flags.SetRolloutPercentageAsync("new-api", 10);

// Day 5: Full rollout
await flags.EnableAsync("new-api");

// Instant rollback if needed
await flags.DisableAsync("new-api");
```

### Exception Handling
```csharp
try
{
    await service.TryAcquireAsync(key);
}
catch (ArgumentNullException ex)
{
    logger.LogWarning(ex, "Invalid key");
    // Handle validation error
}
catch (InvalidOperationException ex)
{
    logger.LogError(ex, "Service error");
    // Fail gracefully
}
```

---

## Files Location

| Component | File |
|-----------|------|
| Rate Limiter | `src/SharedCommon.Resiliency/src/RateLimiting/` |
| Feature Flags | `src/SharedCommon.FeatureFlags/src/Distributed/` |
| Poison Handler | `src/SharedCommon.Messaging/src/DeadLetterQueue/` |
| Migrations | `src/SharedCommon.Core/src/Database/` |
| Configuration | `src/SharedCommon.Core/src/Configuration/` |
| Tests | `tests/SharedCommon.*.UnitTests/` |

---

## Troubleshooting

**Rate limiter not working?**
- Check Redis: `redis-cli ping`
- Check config has Limit > 0 and WindowSeconds > 0
- Review logs for Redis errors

**Feature flag always false?**
- Check flag exists: `await flags.GetFlagAsync("name")`
- Check percentage rollout
- Review logs for evaluation

**Migration hanging?**
- SQL Server 2016+ required
- Monitor transaction log size
- Check for blocking locks

**Config validation fails?**
- Verify all required keys in appsettings.json
- JWT key must be 32+ characters
- CORS origins must use HTTPS in production

---

## Monitoring

```
Rate Limiting:
  - ratelimit.rejected_total
  - ratelimit.window_reset_seconds

Feature Flags:
  - featureflag.evaluations_total
  - featureflag.rollout_percentage

Messages:
  - poisonmessage.dlq_routed_total
  - poisonmessage.retry_attempts

Config:
  - config_validation_errors_total
```

---

## Next Steps

1. Add `using SharedCommon.Configuration;` to Program.cs
2. Copy patterns from [Program.cs.example](../../samples/Program.cs.example)
3. Update appsettings.json with required config
4. Run `dotnet build` to verify
5. Run `dotnet test` to verify tests pass
6. Review [ENTERPRISE-PATTERNS-IMPLEMENTATION.md](../guides/ENTERPRISE-PATTERNS-IMPLEMENTATION.md) for details

---

## API Reference

### IDistributedRateLimiter
```csharp
Task<RateLimitResult> TryAcquireAsync(string key, int tokensToAcquire = 1)
Task ResetAsync(string key)
Task<RateLimitStatus> GetStatusAsync(string key)
```

### IDistributedFeatureFlagService
```csharp
Task<bool> IsEnabledAsync(string flagName, string? userId = null)
Task<FeatureFlagDefinition?> GetFlagAsync(string flagName)
Task EnableAsync(string flagName)
Task DisableAsync(string flagName)
Task SetRolloutPercentageAsync(string flagName, int percentage)
```

### IPoisonMessageHandler
```csharp
Task<MessageProcessingResult> ProcessWithRetryAsync(
    string messageId, 
    string messageType, 
    Func<Task> processAsync,
    Func<Exception, bool>? shouldRetry = null)
```

### IZeroDowntimeMigrationService
```csharp
Task<MigrationResult> AddNullableColumnAsync(...)
Task<MigrationResult> BackfillColumnAsync(...)
Task<MigrationResult> CreateIndexAsync(...)
```

### IEnterpriseConfigurationValidator
```csharp
void ValidateAll()
void ValidateRedisConfiguration()
void ValidateDatabaseConfiguration()
void ValidateAuthenticationConfiguration()
```

---

**Last Updated:** 2024  
**Version:** 1.0  
**Status:** ✅ Production Ready
