# Enterprise Pattern Implementation Guide

## Overview

This guide demonstrates how to implement production-grade enterprise patterns in the SharedCommon platform. Each pattern includes:

- **Interface definitions** - Clear contracts
- **Sealed implementations** - Prevent accidental subclassing
- **Full XML documentation** - IntelliSense support
- **Exception handling** - All exceptions documented
- **Unit tests** - Comprehensive coverage
- **DI integration** - Single-line registration

## Patterns Implemented

### 1. Distributed Rate Limiting

**Purpose:** Protect services from overload with token bucket algorithm backed by Redis.

**Files:**
- Interface: `src/SharedCommon.Resiliency/src/RateLimiting/IDistributedRateLimiter.cs`
- Implementation: `src/SharedCommon.Resiliency/src/RateLimiting/DistributedRateLimiter.cs`
- Middleware: `src/SharedCommon.Resiliency/src/RateLimiting/RateLimitingMiddleware.cs`
- Tests: `tests/SharedCommon.Resiliency.UnitTests/RateLimiting/DistributedRateLimiterTests.cs`

**Usage in Program.cs:**

```csharp
builder.Services.AddDistributedRateLimiting(
    builder.Configuration,
    options => 
    {
        options.Limit = 100;           // 100 requests
        options.WindowSeconds = 60;    // per minute
        options.StrictMode = true;     // fail closed in production
    });

app.UseDistributedRateLimiting();  // Add to middleware pipeline
```

**Configuration (appsettings.json):**

```json
{
  "RateLimit": {
    "Enabled": true,
    "Limit": 100,
    "WindowSeconds": 60,
    "StrictMode": true
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**In Controllers/Endpoints:**

```csharp
app.MapGet("/api/data", async (IDistributedRateLimiter limiter) =>
{
    var result = await limiter.TryAcquireAsync("user-123");
    
    if (!result.Allowed)
        return Results.StatusCode(429);
        
    return Results.Ok(new { Remaining = result.TokensRemaining });
});
```

**Key Features:**
- Atomic Redis operations via Lua scripts (race-free)
- Per-user, per-API-key, or per-IP limiting
- Fail-open pattern (allows traffic if Redis unavailable)
- X-RateLimit-* headers on all responses
- Retry-After header when rate limited
- Exempt paths: /health, /metrics, /swagger

---

### 2. Distributed Feature Flags

**Purpose:** Control feature rollout without redeployment using percentage-based canary deployments.

**Files:**
- Interface: `src/SharedCommon.FeatureFlags/src/Distributed/IDistributedFeatureFlagService.cs`
- Implementation: `src/SharedCommon.FeatureFlags/src/Distributed/DistributedFeatureFlagService.cs`
- Tests: `tests/SharedCommon.FeatureFlags.UnitTests/Distributed/DistributedFeatureFlagServiceTests.cs`

**Usage in Program.cs:**

```csharp
builder.Services.AddDistributedFeatureFlags();
```

**In Controllers/Endpoints:**

```csharp
app.MapGet("/api/experimental", async (
    IDistributedFeatureFlagService flags,
    HttpContext ctx) =>
{
    var userId = ctx.User?.FindFirst("sub")?.Value;
    
    // Check if feature enabled for this user
    var isEnabled = await flags.IsEnabledAsync("new-dashboard", userId);
    
    if (!isEnabled)
        return Results.NotFound();
        
    return Results.Ok(new { /* experimental feature */ });
});
```

**Canary Deployment Example:**

```csharp
// Roll out to 10% of users gradually
await flags.SetRolloutPercentageAsync("new-payment-service", 10);

// Whitelist VIP users to test early
await flags.AllowUserAsync("new-payment-service", "user-vip-1");
await flags.AllowUserAsync("new-payment-service", "user-vip-2");

// Full rollout when confident
await flags.EnableAsync("new-payment-service");

// Instant rollback if issues
await flags.DisableAsync("new-payment-service");
```

**Key Features:**
- Deterministic per-user evaluation (consistent across requests)
- Percentage-based rollouts (0-100%)
- User whitelisting (force enable for specific users)
- User blacklisting (force disable)
- Priority: blacklist > whitelist > percentage > default
- Complements Microsoft.FeatureManagement (config-based flags)

---

### 3. Poison Message Handler

**Purpose:** Resilient message processing with automatic retry and dead letter queue routing.

**Files:**
- Interface: `src/SharedCommon.Messaging/src/DeadLetterQueue/IPoisonMessageHandler.cs`
- Implementation: `src/SharedCommon.Messaging/src/DeadLetterQueue/PoisonMessageHandler.cs`

**Usage in Program.cs:**

```csharp
builder.Services.AddPoisonMessageHandling(
    options =>
    {
        options.MaxRetries = 3;
        options.InitialDelayMs = 100;  // Start with 100ms delay
        options.DLQRetention = TimeSpan.FromDays(30);
    });
```

**In Message Consumers:**

```csharp
public class OrderEventConsumer
{
    private readonly IPoisonMessageHandler _poisonHandler;

    public async Task Handle(OrderCreatedEvent message)
    {
        var result = await _poisonHandler.ProcessWithRetryAsync(
            messageId: message.Id,
            messageType: typeof(OrderCreatedEvent).Name,
            processAsync: async () =>
            {
                // Your business logic here
                await _orderService.ProcessAsync(message);
            },
            shouldRetry: ex => ex is TimeoutException or InvalidOperationException);
        
        if (result.RoutedToDLQ)
        {
            _logger.LogError("Message routed to DLQ after {Attempts} attempts", 
                result.AttemptsUsed);
        }
    }
}
```

**Key Features:**
- Exponential backoff: delay = InitialDelayMs * 2^(attempt-1)
- Smart exception filtering (retryable vs non-retryable)
- Automatic DLQ routing after max retries
- Full exception tracking and logging
- DLQ message retention (default 30 days)

---

### 4. Zero-Downtime Migrations

**Purpose:** Safe schema changes without service interruption using SQL Server ONLINE DDL.

**Files:**
- Interface: `src/SharedCommon.Core/src/Database/IZeroDowntimeMigrationService.cs`
- Implementation: `src/SharedCommon.Core/src/Database/ZeroDowntimeMigrationService.cs`

**Usage in Program.cs:**

```csharp
builder.Services.AddZeroDowntimeMigrations(
    builder.Configuration.GetConnectionString("DefaultConnection"));
```

**In Database Setup:**

```csharp
public class DatabaseInitializer
{
    private readonly IZeroDowntimeMigrationService _migrations;

    public async Task EnsureCreatedAsync()
    {
        // Add new nullable column without table lock
        var result = await _migrations.AddNullableColumnAsync(
            tableName: "Orders",
            columnName: "ProcessedAt",
            columnType: "DATETIME2");
        
        // Backfill existing rows
        await _migrations.BackfillColumnAsync(
            tableName: "Orders",
            columnName: "ProcessedAt",
            updateSql: "SET ProcessedAt = CreatedAt",
            batchSize: 10000,
            delayMs: 100);  // Batched with delays to avoid transaction log overflow
        
        // Create index
        await _migrations.CreateIndexAsync(
            tableName: "Orders",
            indexName: "IX_Orders_ProcessedAt",
            columns: new[] { "ProcessedAt" });
    }
}
```

**Key Features:**
- ONLINE=ON for all DDL operations (no table locks)
- Idempotent migrations (safe to retry)
- Batched backfill with progress tracking
- Automatic delays to prevent transaction log flooding
- SQL Server 2016+ required

---

### 5. Enterprise Configuration Validation

**Purpose:** Fail-fast startup with validation of all required configuration.

**Files:**
- Implementation: `src/SharedCommon.Core/src/Configuration/EnterpriseConfigurationExtensions.cs`
- Tests: `tests/SharedCommon.Core.UnitTests/Configuration/EnterpriseConfigurationValidatorTests.cs`

**Usage in Program.cs:**

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Register validator
builder.Services.AddEnterpriseConfigurationValidation(builder.Configuration);

var app = builder.Build();

// Validate all config before starting (throws if invalid)
app.Services.ValidateStartupConfiguration();

await app.RunAsync();
```

**Validates:**
- ✅ Database connection string present and valid
- ✅ Redis connection string present
- ✅ JWT key (minimum 32 characters)
- ✅ JWT issuer and audience
- ✅ CORS origins (no wildcards in production)
- ✅ OpenTelemetry service name and environment
- ✅ Sampling rate (0-1)
- ✅ Rate limit values (positive integers)

**Configuration (appsettings.json):**

```json
{
  "Jwt": {
    "Key": "your-very-long-secret-key-of-at-least-32-characters-for-hs256",
    "Issuer": "https://myservice.com",
    "Audience": "myapi"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://example.com",
      "https://app.example.com"
    ]
  },
  "Observability": {
    "ServiceName": "OrderService",
    "Environment": "production",
    "SamplingRate": 0.1
  },
  "RateLimit": {
    "Enabled": true,
    "Limit": 100,
    "WindowSeconds": 60
  }
}
```

---

## Architecture Principles

### 1. Sealed Implementations
All service implementations are `sealed` to prevent accidental subclassing:

```csharp
public sealed class DistributedRateLimiter : IDistributedRateLimiter
{
    // Implementation - cannot be subclassed
}
```

**Why:** Forces extension through composition, not inheritance.

### 2. XML Documentation
All public APIs require comprehensive XML documentation:

```csharp
/// <summary>
/// Attempts to acquire rate limit tokens for the given key.
/// </summary>
/// <param name="key">The rate limit key (user ID, API key, or IP)</param>
/// <param name="tokensToAcquire">Number of tokens to acquire (default 1)</param>
/// <returns>Rate limit result with status and retry-after info</returns>
/// <exception cref="ArgumentNullException">Thrown if key is null or empty</exception>
/// <exception cref="ArgumentOutOfRangeException">Thrown if tokensToAcquire is not positive</exception>
public async Task<RateLimitResult> TryAcquireAsync(string key, int tokensToAcquire = 1)
{
    // Implementation
}
```

### 3. Exception Documentation
All exceptions are documented in `<exception>` tags:

```csharp
/// <exception cref="ArgumentNullException">
/// Thrown when the key parameter is null or empty.
/// This prevents DLQ routing failures.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown when Redis connection fails in strict mode.
/// Check Redis connectivity and retry.
/// </exception>
```

### 4. Dependency Injection
All services are registered with clear DI patterns:

```csharp
// Single-line registration
builder.Services.AddDistributedRateLimiting(builder.Configuration);

// Clear service implementation
services.AddSingleton<IDistributedRateLimiter, DistributedRateLimiter>();
```

### 5. Configuration-Driven
No hardcoded values. All configuration through appsettings.json:

```csharp
// ❌ WRONG
var limit = 100;  // Hardcoded

// ✅ CORRECT
var limit = int.Parse(configuration["RateLimit:Limit"] ?? "100");
```

### 6. Comprehensive Testing
All services have unit tests with mocked dependencies:

```csharp
[Fact]
public async Task TryAcquireAsync_WhenRateLimited_ReturnsDenied()
{
    // Arrange
    var redis = new Mock<IDatabase>();
    redis.Setup(r => r.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<RedisKey[]>(), 
        It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
        .ReturnsAsync((long)0);  // 0 = denied

    var service = new DistributedRateLimiter(
        multiplexer: mockMultiplexer.Object,
        options: new RateLimiterOptions(),
        logger: mockLogger.Object);

    // Act
    var result = await service.TryAcquireAsync("test-key");

    // Assert
    Assert.False(result.Allowed);
}
```

---

## Integration Checklist

- [ ] Read `CLAUDE.md` for architecture principles
- [ ] Review `.csproj` files to ensure correct subdirectory structure
- [ ] Update `Directory.Packages.props` with required NuGet versions
- [ ] Copy `Program.cs.example` patterns to your `Program.cs`
- [ ] Register all services via DI
- [ ] Configure `appsettings.json` with all required values
- [ ] Run `dotnet build` to verify compilation
- [ ] Run `dotnet test` to verify unit tests pass
- [ ] Add integration tests for real Redis/database connections
- [ ] Update API documentation with new endpoints
- [ ] Review security implications (rate limits, CORS, JWT)
- [ ] Set up monitoring and alerts for rate limit violations
- [ ] Test feature flag rollout in staging environment
- [ ] Load test to validate rate limit tuning

---

## Common Patterns

### Pattern: Dependency Injection with Configuration

```csharp
builder.Services.AddDistributedRateLimiting(
    builder.Configuration,
    options =>
    {
        // Load from configuration
        options.Limit = int.Parse(
            builder.Configuration["RateLimit:Limit"] ?? "100");
        
        // Override for specific environments
        if (builder.Environment.IsProduction())
            options.StrictMode = true;
    });
```

### Pattern: Using Sealed Classes

```csharp
// Library code defines interface and sealed implementation
public interface IMyService { }
public sealed class MyService : IMyService { }

// Client code registers and uses interface
builder.Services.AddSingleton<IMyService, MyService>();
var service = app.Services.GetRequiredService<IMyService>();
```

### Pattern: Exception Handling with Specific Exceptions

```csharp
try
{
    await service.TryAcquireAsync(key);
}
catch (ArgumentNullException ex)
{
    logger.LogWarning(ex, "Invalid rate limit key");
}
catch (InvalidOperationException ex)
{
    logger.LogError(ex, "Rate limiter failure - falling back");
}
```

### Pattern: Feature Flags for Gradual Rollout

```csharp
// Day 1: Test with 1% of users
await flags.SetRolloutPercentageAsync("feature", 1);

// Day 3: Increase to 10%
await flags.SetRolloutPercentageAsync("feature", 10);

// Day 5: Full rollout to all users
await flags.EnableAsync("feature");

// Instant rollback if issues
await flags.DisableAsync("feature");
```

---

## Troubleshooting

### Rate Limiter Returns 0 Tokens

**Problem:** All requests are rate limited immediately

**Solution:**
1. Check Redis connection: `redis-cli ping`
2. Verify rate limit configuration (Limit > 0, WindowSeconds > 0)
3. Check Redis key expiration: `redis-cli TTL "ratelimit:user-123"`
4. Review logs for Redis errors

### Feature Flag Not Evaluating

**Problem:** Feature flag always returns false despite being enabled

**Solution:**
1. Check flag exists: `await flags.GetFlagAsync("flag-name")`
2. Verify enabled status: `await flags.IsEnabledAsync("flag-name", userId)`
3. Check user whitelist/blacklist
4. Verify percentage rollout logic with deterministic hash

### Migrations Hanging

**Problem:** Zero-downtime migration never completes

**Solution:**
1. Check SQL Server version (requires 2016+)
2. Monitor transaction log size (backfill uses space)
3. Increase batch size gradually if blocking locks occur
4. Run during maintenance window if performance critical

---

## Performance Tuning

### Rate Limiting

- **High-traffic services:** Increase Limit or reduce WindowSeconds
- **Public endpoints:** Start conservative (10 req/min) and increase
- **Private APIs:** Higher limit (1000+ req/min)

### Feature Flags

- **Many flags:** Use Redis cache (already implemented)
- **Frequent checks:** Flag status is cached for 60 seconds
- **Percentage rollout:** Uses consistent hashing (no randomness)

### Message Handling

- **High-volume messages:** Increase MaxRetries carefully
- **Long backoff:** Use exponential backoff to avoid storms
- **DLQ processing:** Monitor DLQ size for accumulation

---

## References

- [Rate Limiting Best Practices](https://cloud.google.com/architecture/rate-limiting-strategies-techniques)
- [Feature Flags in Production](https://featureflags.io/feature-flag-best-practices/)
- [SQL Server Online DDL](https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-table-transact-sql)
- [Zero-Downtime Deployments](https://martinfowler.com/bliki/BlueGreenDeployment.html)
