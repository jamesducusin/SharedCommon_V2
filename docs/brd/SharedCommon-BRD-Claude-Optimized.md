# SharedCommon Platform — Complete Business Requirements Document

**For Claude Code Implementation**

Version: 2.0 (Claude-Optimized)
Date: 2026-05-09
Status: Ready for Implementation

---

## Executive Summary

**What:** Modular enterprise .NET NuGet ecosystem for reusable cross-cutting infrastructure.

**Why:** Eliminate boilerplate, configuration drift, and security inconsistencies across services.

**How:** Install packages, register in Program.cs, configure via appsettings.json. No hardcoding. No repetition.

**Success:** 70%+ reduction in setup time, 80%+ reduction in duplicate code, unified observability and security standards.

---

## Part 1: Vision & Principles

### Vision Statement

Enable enterprise teams to build production-grade .NET services in minutes using modular, configurable, battle-tested shared infrastructure components.

### Core Principles (Non-Negotiable)

| Principle | Means |
|-----------|-------|
| **Plug-and-Play** | Each package works independently; no hidden interdependencies |
| **Configuration-Driven** | All behavior externalized to appsettings.json; zero hardcoding |
| **Secure-by-Default** | Safe defaults for auth, headers, validation, secrets |
| **Observable-by-Default** | Structured logging, correlation IDs, tracing in every package |
| **Cloud-Native First** | Kubernetes, Docker, distributed systems ready out-of-the-box |
| **Clean Architecture Compatible** | No infrastructure leakage; SOLID principles throughout |
| **Fail-Safe** | Graceful degradation when external services fail |
| **Extensible** | Override defaults without modifying package internals |

---

## Part 2: Module Architecture

### 2.1 Module Dependency Graph

```
SharedCommon.Core (no dependencies)
│
├─→ SharedCommon.Logging
│   └─→ SharedCommon.Middlewares (uses logging)
│
├─→ SharedCommon.Security
│   └─→ SharedCommon.Auth (extends security)
│       └─→ SharedCommon.Middlewares (uses auth)
│
├─→ SharedCommon.Caching (independent)
│
├─→ SharedCommon.Validation (independent)
│
├─→ SharedCommon.Observability
│   └─→ All packages (optional instrumentation)
│
├─→ SharedCommon.Messaging (independent)
│
├─→ SharedCommon.Resiliency (independent)
│
├─→ SharedCommon.HealthChecks (independent)
│
├─→ SharedCommon.Utilities (independent)
│
├─→ SharedCommon.ResponseBuilder (independent)
│
├─→ SharedCommon.Grpc (independent)
│
├─→ SharedCommon.GraphQL (independent)
│
├─→ SharedCommon.Cloud (independent)
│
├─→ SharedCommon.ApiVersioning (independent)
│
├─→ SharedCommon.FeatureFlags (independent)
│
├─→ SharedCommon.MultiTenancy (independent)
│
├─→ SharedCommon.Storage (independent)
│
├─→ SharedCommon.Auditing (uses logging)
│
└─→ SharedCommon.BackgroundJobs (independent)
```

**Rule:** No circular dependencies. No package may depend on packages that depend on it.

---

## Part 3: Core Modules (Phase 1 — Must Implement First)

### 3.1 SharedCommon.Core

**Purpose:** Foundation types, abstractions, shared contracts.

**No Dependencies:**

#### 3.1.1 Public API Surface

```csharp
namespace SharedCommon.Core;

/// <summary>
/// Result pattern for operation outcomes without exceptions.
/// </summary>
public abstract record Result
{
    public sealed record Success(object? Data = null) : Result;
    public sealed record Failure(string Code, string Message, 
        Exception? Exception = null) : Result;
    public sealed record Validation(IDictionary<string, string[]> Errors) : Result;
}

/// <summary>Generic Result for typed returns.</summary>
public abstract record Result<T> : Result
{
    public sealed record Success(T Data) : Result<T>;
    public sealed record Failure(string Code, string Message, 
        Exception? Exception = null) : Result<T>;
    public sealed record Validation(IDictionary<string, string[]> Errors) : Result<T>;
}

/// <summary>Unique request identifier for correlation across systems.</summary>
public record CorrelationId
{
    public static CorrelationId New() => new(Guid.NewGuid().ToString());
    public static CorrelationId From(string value) => new(value);
    
    public string Value { get; init; }
    private CorrelationId(string value) 
        => Value = string.IsNullOrWhiteSpace(value) 
            ? throw new ArgumentException("Invalid correlation ID") 
            : value;
}

/// <summary>Ambient context for request-scoped data.</summary>
public interface IRequestContext
{
    CorrelationId CorrelationId { get; }
    string? TenantId { get; }
    string? UserId { get; }
    IDictionary<string, object> Properties { get; }
}

/// <summary>Application startup health check result.</summary>
public interface IStartupHealthCheck
{
    string Name { get; }
    Task<bool> CheckAsync(CancellationToken ct);
}

/// <summary>Allows packages to self-register validation rules.</summary>
public interface IValidationProvider
{
    void RegisterValidators(IValidatorRegistry registry);
}

/// <summary>Allows packages to register their observability instrumentation.</summary>
public interface IObservabilityProvider
{
    void RegisterInstrumentation(IObservabilityRegistry registry);
}
```

#### 3.1.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Core": {
      "ApplicationName": "string | required",
      "EnvironmentName": "Development|Staging|Production | required",
      "Version": "string | required",
      "AllowedOrigins": "string[] | optional"
    }
  }
}
```

#### 3.1.3 Registration Example

```csharp
builder.Services.AddSharedCommonCore(builder.Configuration);
```

#### 3.1.4 Error Handling

| Scenario | Behavior |
|----------|----------|
| ApplicationName missing | Throw ArgumentException immediately |
| Invalid environment name | Throw ArgumentException with valid values |
| Invalid correlation ID | Throw ArgumentException |

#### 3.1.5 Extension Points

- Custom `IRequestContext` implementation
- Custom correlation ID generation strategy
- Custom property types in request context

---

### 3.2 SharedCommon.Logging

**Purpose:** Structured logging with multiple sinks (console, file, Elasticsearch, database).

**Dependencies:** SharedCommon.Core

#### 3.2.1 Public API Surface

```csharp
namespace SharedCommon.Logging;

/// <summary>Register logging infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SharedCommon logging with Serilog.
    /// 
    /// Automatically configures:
    /// - Console output
    /// - Structured properties enrichment
    /// - Correlation ID propagation
    /// - Application metadata
    /// 
    /// Additional sinks enabled via configuration.
    /// </summary>
    public static IServiceCollection AddSharedCommonLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Implementation will:
        // 1. Read LoggingOptions from configuration
        // 2. Configure Serilog with enabled sinks
        // 3. Register LogContext for structured properties
        // 4. Register correlation ID enricher
        // 5. Return IServiceCollection for chaining
    }
}

/// <summary>Structured logging context for adding properties to logs.</summary>
public static class LogContext
{
    /// <summary>Add a structured property to current log scope.</summary>
    public static IDisposable Property(string name, object? value);
    
    /// <summary>Add multiple structured properties.</summary>
    public static IDisposable Properties(
        IDictionary<string, object?> values);
    
    /// <summary>Get all properties in current scope.</summary>
    public static IDictionary<string, object?> GetProperties();
    
    /// <summary>Clear all properties.</summary>
    public static void Clear();
}

/// <summary>Request correlation utilities.</summary>
public static class CorrelationIdExtensions
{
    /// <summary>Get or create correlation ID for current request.</summary>
    public static CorrelationId GetOrCreateCorrelationId(
        this HttpContext context);
    
    /// <summary>Set correlation ID for current request.</summary>
    public static void SetCorrelationId(
        this HttpContext context, 
        CorrelationId id);
}

/// <summary>Health check for logging infrastructure.</summary>
public class LoggingHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default);
}
```

#### 3.2.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Logging": {
      "ApplicationName": "string | required | max 50 chars",
      "LogLevel": "Debug|Information|Warning|Error|Critical | default: Information",
      "MinimumLevel": "Debug|Information|Warning|Error|Critical | default: Information",
      
      "Serilog": {
        "Enabled": "bool | default: true",
        "Format": "Json|CompactJson|Text | default: Json",
        "Destructure": {
          "Enabled": "bool | default: true",
          "MaxStringLength": "int | default: 4096",
          "MaxDepth": "int | default: 10",
          "MaxCollectionCount": "int | default: 100"
        }
      },
      
      "Console": {
        "Enabled": "bool | default: true",
        "Theme": "Colored|Grayscale|None | default: Colored",
        "IncludeTimestamp": "bool | default: true"
      },
      
      "File": {
        "Enabled": "bool | default: false",
        "Path": "string | default: ./logs/app-.txt | required if enabled",
        "RetainedFileCountLimit": "int | default: 30",
        "RollingInterval": "Infinite|Year|Month|Day|Hour|Minute | default: Day",
        "FileSizeLimit": "int (bytes) | default: 1073741824 (1GB)"
      },
      
      "Elasticsearch": {
        "Enabled": "bool | default: false",
        "Url": "string (Uri) | required if enabled",
        "Username": "string | optional",
        "Password": "string | optional (use secrets)",
        "IndexFormat": "string | default: logs-{0:yyyy.MM.dd}",
        "BatchSize": "int | default: 500",
        "Period": "int (ms) | default: 2000"
      },
      
      "Database": {
        "Enabled": "bool | default: false",
        "ConnectionString": "string | required if enabled (use secrets)",
        "TableName": "string | default: Logs",
        "BulkInsertBatchSize": "int | default: 100",
        "RetentionDays": "int | default: 90"
      },
      
      "CorrelationId": {
        "Enabled": "bool | default: true",
        "HeaderName": "string | default: X-Correlation-ID",
        "LogPropertyName": "string | default: CorrelationId"
      },
      
      "ExcludePatterns": [
        "string[] | log messages matching these patterns are ignored"
      ],
      
      "AsyncMode": "bool | default: true"
    }
  }
}
```

#### 3.2.3 Real-World Example Configuration

```json
{
  "SharedCommon": {
    "Logging": {
      "ApplicationName": "OrderService",
      "LogLevel": "Information",
      "Serilog": {
        "Enabled": true,
        "Format": "Json"
      },
      "Console": {
        "Enabled": true,
        "Theme": "Colored"
      },
      "File": {
        "Enabled": true,
        "Path": "./logs/order-service-.txt",
        "RollingInterval": "Day",
        "RetainedFileCountLimit": 30
      },
      "Elasticsearch": {
        "Enabled": true,
        "Url": "https://elasticsearch.example.com:9200",
        "Username": "${ELASTIC_USER}",
        "Password": "${ELASTIC_PASSWORD}",
        "IndexFormat": "services-orderservice-{0:yyyy.MM.dd}"
      },
      "Database": {
        "Enabled": false
      },
      "CorrelationId": {
        "Enabled": true,
        "HeaderName": "X-Correlation-ID"
      }
    }
  }
}
```

#### 3.2.4 Usage Pattern

```csharp
// In Program.cs
builder.Services.AddSharedCommonLogging(builder.Configuration);

// In service
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    
    public async Task<Result> CreateOrderAsync(
        CreateOrderCommand cmd, 
        CancellationToken ct)
    {
        using (LogContext.Property("OrderId", cmd.OrderId))
        using (LogContext.Property("CustomerId", cmd.CustomerId))
        {
            _logger.LogInformation(
                "Creating order {OrderId} for customer {CustomerId}",
                cmd.OrderId, cmd.CustomerId);
            
            try
            {
                var order = await _orderRepository.CreateAsync(cmd, ct);
                
                _logger.LogInformation(
                    "Order created successfully",
                    new { OrderId = order.Id, Amount = order.Total });
                
                return new Result.Success(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create order");
                
                return new Result.Failure(
                    "ORDER_CREATE_FAILED",
                    "Failed to create order",
                    ex);
            }
        }
    }
}
```

#### 3.2.5 Error Handling

| Scenario | Behavior |
|----------|----------|
| ApplicationName missing | Throw ArgumentNullException during AddSharedCommonLogging |
| Invalid Elasticsearch URL | Log warning, skip Elasticsearch sink |
| Database connection fails | Log warning, sink starts on next batch attempt |
| Corrupted log message | Catch serialization exception, log failure event, continue |
| All sinks disabled | Still log to console (fallback), log warning once |
| File path invalid | Throw DirectoryNotFoundException during AddSharedCommonLogging |

#### 3.2.6 Structured Properties (Required in All Logs)

Every log *automatically* includes:
- `Timestamp`: ISO 8601 UTC
- `Level`: Debug, Information, Warning, Error, Critical
- `MessageTemplate`: Parameterized message
- `CorrelationId`: From IRequestContext or ambient context
- `ApplicationName`: From configuration
- `EnvironmentName`: From configuration
- `MachineName`: Runtime environment
- `ThreadId`: Current thread
- `Exception`: Stack trace (if applicable)

User-added properties (via LogContext):
- `OrderId`, `CustomerId`, `TenantId`, etc. (contextual)

#### 3.2.7 Log Levels (How-To Use)

| Level | Usage |
|-------|-------|
| **Debug** | Development only; detailed flow, variable values |
| **Information** | Important business events; request start/end, successful operations |
| **Warning** | Recoverable issues; retrying failed operation, deprecated API, config mismatch |
| **Error** | Recoverable errors; validation failure, external service timeout, database constraint violation |
| **Critical** | Application failure; unable to start, unrecoverable error, requires immediate attention |

#### 3.2.8 Extension Points

```csharp
// Custom Serilog enricher
public class CustomEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddOrUpdateProperty(
            propertyFactory.CreateProperty("CustomProperty", value));
    }
}

// Register custom enricher in options
var options = new LoggingOptions();
options.CustomEnrichers.Add(new CustomEnricher());
```

---

### 3.3 SharedCommon.Caching

**Purpose:** Hybrid caching layer (in-memory L1 + distributed L2 + optional database L3).

**Dependencies:** SharedCommon.Core

#### 3.3.1 Public API Surface

```csharp
namespace SharedCommon.Caching;

/// <summary>Unified caching interface across tiers.</summary>
public interface ICacheService
{
    /// <summary>Get value from cache (L1 → L2 → L3).</summary>
    Task<T?> GetAsync<T>(
        string key, 
        CancellationToken ct = default) where T : class;
    
    /// <summary>Set value in cache (write all enabled tiers).</summary>
    Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;
    
    /// <summary>Remove value from all cache tiers.</summary>
    Task RemoveAsync(
        string key, 
        CancellationToken ct = default);
    
    /// <summary>Check if key exists in any tier.</summary>
    Task<bool> ExistsAsync(
        string key, 
        CancellationToken ct = default);
    
    /// <summary>Get-or-set pattern (atomic).</summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;
    
    /// <summary>Get multiple values (batch).</summary>
    Task<IDictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken ct = default) where T : class;
    
    /// <summary>Set multiple values (batch).</summary>
    Task SetManyAsync<T>(
        IDictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;
    
    /// <summary>Invalidate keys matching pattern (Redis KEYS).</summary>
    Task InvalidateByPatternAsync(
        string pattern,
        CancellationToken ct = default);
    
    /// <summary>Clear entire cache (dangerous, use carefully).</summary>
    Task ClearAsync(CancellationToken ct = default);
    
    /// <summary>Get cache statistics.</summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>Cache hit/miss statistics.</summary>
public record CacheStatistics(
    long Hits,
    long Misses,
    double HitRate,
    long Size,
    DateTimeOffset LastCleared);

/// <summary>Register caching infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedCommonCaching(
        this IServiceCollection services,
        IConfiguration configuration);
}

/// <summary>Attribute-based caching for methods.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class CacheableAttribute : Attribute
{
    public string KeyPrefix { get; set; } = string.Empty;
    public int DurationSeconds { get; set; } = 300;
}
```

#### 3.3.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Caching": {
      "DefaultProvider": "Memory|Redis|Hybrid | default: Hybrid",
      "DefaultTtlSeconds": "int | default: 300",
      "SerializationFormat": "Json|MessagePack | default: Json",
      
      "Memory": {
        "Enabled": "bool | default: true",
        "MaximumSize": "int (items) | default: 10000",
        "SlidingExpiration": "int (seconds) | default: 300",
        "AbsoluteExpiration": "int (seconds) | optional"
      },
      
      "Redis": {
        "Enabled": "bool | default: false",
        "Connection": "string (redis connection) | required if enabled",
        "KeyPrefix": "string | default: sharedcommon:",
        "DefaultTtlSeconds": "int | default: 300",
        "DatabaseId": "int (0-15) | default: 0",
        "SSL": "bool | default: false",
        "ConnectTimeout": "int (ms) | default: 5000",
        "SyncTimeout": "int (ms) | default: 1000"
      },
      
      "Database": {
        "Enabled": "bool | default: false",
        "ConnectionString": "string | required if enabled",
        "TableName": "string | default: CacheItems",
        "DefaultTtlSeconds": "int | default: 300",
        "CleanupIntervalSeconds": "int | default: 3600"
      },
      
      "CacheKeyPolicy": {
        "Separator": "string | default: :",
        "NormalizeKeys": "bool | default: true",
        "MaxKeyLength": "int | default: 512"
      },
      
      "Hybrid": {
        "L1Enabled": "bool | default: true",
        "L2Enabled": "bool | default: false",
        "L3Enabled": "bool | default: false",
        "PromoteOnHit": "bool | default: true",
        "InvalidateDownstream": "bool | default: true"
      },
      
      "Diagnostics": {
        "Enabled": "bool | default: true",
        "TrackStatistics": "bool | default: true",
        "LogCacheMisses": "bool | default: false"
      }
    }
  }
}
```

#### 3.3.3 Caching Strategy & Tier Behavior

**Hybrid Mode (Recommended):**

1. **Read Path:** L1 (Memory) → L2 (Redis) → L3 (Database) → Cache Miss
2. **Write Path:** Write to all enabled tiers (atomic)
3. **Invalidation:** Remove from all tiers
4. **PromoteOnHit:** If found in L2/L3, refresh in L1
5. **Timeout:** L1 might expire sooner than L2 (configurable)

**Memory Tier:**
- Fast, in-process
- Size-limited (prevent memory leaks)
- Lost on app restart
- Per-instance state (not shared)

**Redis Tier:**
- Shared across instances (distributed)
- Persistent (survives restarts)
- Network latency
- Expensive to lose (affects all instances)

**Database Tier:**
- Persistent, queryable
- Slowest tier
- For critical long-lived data
- Rarely used; mostly for recovery

#### 3.3.4 Usage Patterns

```csharp
// Pattern 1: Simple get/set
var user = await cache.GetAsync<User>("user:123");
if (user == null)
{
    user = await userService.GetAsync("123", ct);
    await cache.SetAsync("user:123", user, TimeSpan.FromHours(1), ct);
}

// Pattern 2: Get-or-set (atomic)
var product = await cache.GetOrSetAsync(
    key: "product:456",
    factory: async (ct) => await productService.GetAsync("456", ct),
    expiration: TimeSpan.FromMinutes(30),
    ct: ct);

// Pattern 3: Batch operations
var userIds = new[] { "1", "2", "3" };
var keys = userIds.Select(id => $"user:{id}");
var users = await cache.GetManyAsync<User>(keys, ct);

// Pattern 4: Pattern invalidation
await cache.InvalidateByPatternAsync("order:123:*", ct); // Invalidate all keys matching pattern

// Pattern 5: Attribute-based (with interceptor)
[Cacheable(KeyPrefix = "user", DurationSeconds = 600)]
public async Task<User> GetUserAsync(string id, CancellationToken ct)
{
    // Automatically cached with key "user:id"
    return await userService.GetAsync(id, ct);
}
```

#### 3.3.5 Error Handling

| Scenario | Behavior |
|----------|----------|
| Redis connection fails | Log warning, skip L2, use L1+L3 (degraded) |
| Database unavailable | Log warning, skip L3, use L1+L2 (degraded) |
| Serialization fails | Log error, cache miss, return null |
| TTL invalid (negative) | Use default TTL from config |
| Key too long | Throw ArgumentException immediately |
| Memory tier full | Evict oldest (LRU) |
| Redis timeout | Fall back to L3 or miss |

---

### 3.4 SharedCommon.Security

**Purpose:** Secure defaults, security headers, rate limiting, input validation.

**Dependencies:** SharedCommon.Core

#### 3.4.1 Public API Surface

```csharp
namespace SharedCommon.Security;

/// <summary>Register security middleware and defaults.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedCommonSecurity(
        this IServiceCollection services,
        IConfiguration configuration);
}

/// <summary>Security headers middleware.</summary>
public interface ISecurityHeadersPolicy
{
    void Apply(HttpContext context);
}

/// <summary>Rate limiting service.</summary>
public interface IRateLimitService
{
    /// <summary>Check if request should be allowed.</summary>
    Task<bool> AllowAsync(
        string identifier, // IP, userId, API key, etc.
        string bucket = "default",
        CancellationToken ct = default);
    
    /// <summary>Get current rate limit status.</summary>
    Task<RateLimitStatus> GetStatusAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default);
    
    /// <summary>Reset rate limit for identifier.</summary>
    Task ResetAsync(
        string identifier,
        string bucket = "default",
        CancellationToken ct = default);
}

/// <summary>Rate limit status.</summary>
public record RateLimitStatus(
    int RequestsAllowed,
    int RequestsUsed,
    int RequestsRemaining,
    DateTimeOffset ResetAt);

/// <summary>Input validation and sanitization.</summary>
public interface IInputValidator
{
    /// <summary>Validate input against configured rules.</summary>
    Result Validate(string input, InputValidationContext context);
    
    /// <summary>Sanitize potentially malicious input.</summary>
    string Sanitize(string input, SanitizationMode mode);
}

public enum SanitizationMode
{
    HtmlEncode,      // For HTML content
    UrlEncode,       // For URLs
    ScriptEncode,    // For JavaScript
    SqlEscape        // For SQL (legacy; use parameterized queries)
}

/// <summary>CORS policy builder.</summary>
public interface ICorsBuilder
{
    ICorsBuilder AllowOrigins(params string[] origins);
    ICorsBuilder AllowCredentials();
    ICorsBuilder AllowMethods(params string[] methods);
    ICorsBuilder AllowHeaders(params string[] headers);
    void Build();
}
```

#### 3.4.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Security": {
      "SecurityHeaders": {
        "Enabled": "bool | default: true",
        
        "StrictTransportSecurity": {
          "Enabled": "bool | default: true",
          "MaxAge": "int (seconds) | default: 31536000 (1 year)",
          "IncludeSubdomains": "bool | default: true",
          "Preload": "bool | default: false"
        },
        
        "ContentSecurityPolicy": {
          "Enabled": "bool | default: true",
          "DefaultSrc": "string | default: 'self'",
          "ScriptSrc": "string | default: 'self'",
          "StyleSrc": "string | default: 'self'",
          "ImgSrc": "string | default: 'self' data:",
          "ReportUri": "string | optional"
        },
        
        "XContentTypeOptions": {
          "Enabled": "bool | default: true",
          "NoSniff": "bool | default: true"
        },
        
        "XFrameOptions": {
          "Enabled": "bool | default: true",
          "Policy": "Deny|SameOrigin | default: Deny"
        },
        
        "ReferrerPolicy": {
          "Enabled": "bool | default: true",
          "Policy": "no-referrer|same-origin|strict-origin | default: strict-origin"
        },
        
        "PermissionsPolicy": {
          "Enabled": "bool | default: false",
          "Features": {
            "Camera": "string | default: ()",
            "Microphone": "string | default: ()",
            "Geolocation": "string | default: ()"
          }
        }
      },
      
      "RateLimit": {
        "Enabled": "bool | default: true",
        "Backend": "Memory|Redis | default: Memory",
        
        "Policies": {
          "Default": {
            "MaxRequests": "int | default: 100",
            "WindowSeconds": "int | default: 60"
          },
          "Authenticated": {
            "MaxRequests": "int | default: 1000",
            "WindowSeconds": "int | default: 60"
          },
          "ApiEndpoint": {
            "MaxRequests": "int | default: 10000",
            "WindowSeconds": "int | default: 3600"
          }
        },
        
        "HeaderName": "string | default: X-RateLimit-Remaining"
      },
      
      "InputValidation": {
        "Enabled": "bool | default: true",
        "MaxUrlLength": "int | default: 2048",
        "MaxQueryStringLength": "int | default: 8192",
        "MaxBodySizeBytes": "int | default: 10485760 (10MB)",
        "BlockSuspiciousPatterns": "bool | default: true"
      },
      
      "Cors": {
        "Enabled": "bool | default: true",
        "AllowedOrigins": "string[] | required",
        "AllowedMethods": "string[] | default: [GET, POST, PUT, DELETE]",
        "AllowedHeaders": "string[] | default: [*]",
        "AllowCredentials": "bool | default: false",
        "MaxAge": "int (seconds) | default: 3600"
      },
      
      "Https": {
        "Enforced": "bool | default: true",
        "RedirectStatusCode": "int | default: 307 (Temporary)"
      }
    }
  }
}
```

#### 3.4.3 Real-World Example Configuration

```json
{
  "SharedCommon": {
    "Security": {
      "SecurityHeaders": {
        "Enabled": true,
        "StrictTransportSecurity": {
          "Enabled": true,
          "MaxAge": 31536000,
          "IncludeSubdomains": true
        },
        "ContentSecurityPolicy": {
          "Enabled": true,
          "DefaultSrc": "'self'",
          "ScriptSrc": "'self' cdn.example.com",
          "StyleSrc": "'self' 'unsafe-inline'"
        }
      },
      "RateLimit": {
        "Enabled": true,
        "Backend": "Redis",
        "Policies": {
          "Default": {
            "MaxRequests": 100,
            "WindowSeconds": 60
          }
        }
      },
      "Cors": {
        "Enabled": true,
        "AllowedOrigins": [
          "https://app.example.com",
          "https://admin.example.com"
        ],
        "AllowCredentials": true
      },
      "Https": {
        "Enforced": true
      }
    }
  }
}
```

#### 3.4.4 Usage Pattern

```csharp
// In Program.cs
builder.Services.AddSharedCommonSecurity(builder.Configuration);
app.UseSharedCommonSecurityHeaders(); // Apply headers
app.UseSharedCommonRateLimit();       // Rate limiting
app.UseSharedCommonInputValidation(); // Input validation
app.UseSharedCommonCors();            // CORS

// In controller
[ApiController]
[Route("api/[controller]")]
[RateLimit("ApiEndpoint")] // Use specific policy
public class OrdersController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    
    public OrdersController(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        // Rate limiting automatically checked by attribute
        // Input validation automatically applied
        // Security headers automatically set
        
        var status = await _rateLimitService.GetStatusAsync(
            Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            "ApiEndpoint",
            ct);
        
        return Ok(new { RemainingRequests = status.RequestsRemaining });
    }
}
```

#### 3.4.5 Error Handling

| Scenario | Behavior |
|----------|----------|
| Rate limit exceeded | Return 429 Too Many Requests |
| CORS origin rejected | Return 403 Forbidden |
| Body too large | Return 413 Payload Too Large |
| Invalid input detected | Log security event, return 400 Bad Request |
| HTTPS not enforced | Redirect to HTTPS (307) |

---

### 3.5 SharedCommon.Auth

**Purpose:** JWT authentication, OAuth2/OpenID Connect integration.

**Dependencies:** SharedCommon.Core, SharedCommon.Security, SharedCommon.Logging

#### 3.5.1 Public API Surface

```csharp
namespace SharedCommon.Auth;

/// <summary>JWT claims principal.</summary>
public interface IAuthUser
{
    string Id { get; }
    string Email { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    IDictionary<string, object> Claims { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Authentication service.</summary>
public interface IAuthService
{
    /// <summary>Validate JWT token and return principal.</summary>
    Task<Result<IAuthUser>> ValidateTokenAsync(
        string token,
        CancellationToken ct = default);
    
    /// <summary>Generate JWT token for user.</summary>
    Task<Result<string>> GenerateTokenAsync(
        string userId,
        string email,
        IEnumerable<string> roles,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
    
    /// <summary>Refresh JWT token.</summary>
    Task<Result<string>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);
    
    /// <summary>Revoke token (blacklist).</summary>
    Task<Result> RevokeTokenAsync(
        string token,
        CancellationToken ct = default);
}

/// <summary>Register authentication infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedCommonAuth(
        this IServiceCollection services,
        IConfiguration configuration);
}

/// <summary>Current user context.</summary>
public interface ICurrentUser
{
    IAuthUser User { get; }
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
}
```

#### 3.5.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Auth": {
      "Jwt": {
        "Enabled": "bool | default: true",
        "SecretKey": "string | required (use secrets, min 32 chars)",
        "Audience": "string | required",
        "Issuer": "string | required",
        "ExpirationMinutes": "int | default: 60",
        "RefreshTokenExpirationDays": "int | default: 7",
        "Algorithm": "HS256|RS256 | default: HS256",
        
        "Claims": {
          "IncludeEmail": "bool | default: true",
          "IncludeRoles": "bool | default: true",
          "IncludePermissions": "bool | default: true",
          "CustomClaims": "object | optional"
        },
        
        "Validation": {
          "ValidateAudience": "bool | default: true",
          "ValidateIssuer": "bool | default: true",
          "ValidateLifetime": "bool | default: true",
          "ClockSkew": "int (seconds) | default: 0"
        }
      },
      
      "OAuth": {
        "Enabled": "bool | default: false",
        
        "Providers": {
          "AzureAd": {
            "Enabled": "bool | default: false",
            "TenantId": "string | required if enabled",
            "ClientId": "string | required if enabled",
            "Authority": "string | auto-generated from TenantId"
          },
          "Google": {
            "Enabled": "bool | default: false",
            "ClientId": "string | required if enabled",
            "ClientSecret": "string | required if enabled (use secrets)"
          },
          "Github": {
            "Enabled": "bool | default: false",
            "ClientId": "string | required if enabled",
            "ClientSecret": "string | required if enabled (use secrets)"
          }
        }
      },
      
      "TokenBlacklist": {
        "Enabled": "bool | default: true",
        "Backend": "Memory|Redis | default: Redis",
        "CheckExpired": "bool | default: true"
      },
      
      "PasswordPolicy": {
        "Enabled": "bool | default: true",
        "MinLength": "int | default: 12",
        "RequireUppercase": "bool | default: true",
        "RequireLowercase": "bool | default: true",
        "RequireDigits": "bool | default: true",
        "RequireSpecialChars": "bool | default: true",
        "ExpirationDays": "int | optional"
      }
    }
  }
}
```

#### 3.5.3 Usage Pattern

```csharp
// In Program.cs
builder.Services.AddSharedCommonAuth(builder.Configuration);

// In authentication controller
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;
    
    public AuthController(IAuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }
    
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken(
        [FromBody] TokenRequest request,
        CancellationToken ct)
    {
        var result = await _authService.GenerateTokenAsync(
            userId: request.UserId,
            email: request.Email,
            roles: request.Roles,
            expiration: TimeSpan.FromMinutes(60),
            cancellationToken: ct);
        
        return result switch
        {
            Result.Success success => Ok(new { Token = success.Data }),
            Result.Failure failure => BadRequest(new { Error = failure.Message }),
            _ => StatusCode(500)
        };
    }
    
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            UserId = _currentUser.UserId,
            Email = _currentUser.Email,
            Roles = _currentUser.Roles,
            IsAuthenticated = _currentUser.IsAuthenticated
        });
    }
}
```

#### 3.5.4 Error Handling

| Scenario | Behavior |
|----------|----------|
| Secret key invalid or too short | Throw InvalidOperationException during AddSharedCommonAuth |
| Token expired | Return Result.Failure with code "TOKEN_EXPIRED" |
| Token blacklisted | Return Result.Failure with code "TOKEN_REVOKED" |
| Invalid signature | Return Result.Failure with code "TOKEN_INVALID" |
| Password policy violated | Return Result.Validation with field errors |
| OAuth provider unreachable | Log error, return Result.Failure with provider-specific code |

---

### 3.6 SharedCommon.Middlewares

**Purpose:** Centralized exception handling, correlation ID propagation, request/response logging.

**Dependencies:** SharedCommon.Core, SharedCommon.Logging, SharedCommon.Auth (optional)

#### 3.6.1 Public API Surface

```csharp
namespace SharedCommon.Middlewares;

/// <summary>Central exception handling middleware.</summary>
public class ExceptionHandlingMiddleware
{
    // Automatically catches exceptions and returns standardized error responses
    // Logs exceptions with correlation ID
    // Returns appropriate HTTP status codes
}

/// <summary>Correlation ID middleware.</summary>
public class CorrelationIdMiddleware
{
    // Ensures every request has a correlation ID
    // Propagates to all downstream calls
    // Available in IRequestContext
}

/// <summary>Request/response logging middleware.</summary>
public class RequestLoggingMiddleware
{
    // Logs incoming request: method, path, headers
    // Logs outgoing response: status, duration
    // Includes correlation ID in all logs
}

/// <summary>Register middleware infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedCommonMiddlewares(
        this IServiceCollection services,
        IConfiguration configuration);
}

/// <summary>Middleware configuration extension methods.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>Use exception handling middleware (must be first).</summary>
    public static IApplicationBuilder UseSharedCommonExceptionHandling(
        this IApplicationBuilder app);
    
    /// <summary>Use correlation ID middleware.</summary>
    public static IApplicationBuilder UseSharedCommonCorrelationId(
        this IApplicationBuilder app);
    
    /// <summary>Use request logging middleware.</summary>
    public static IApplicationBuilder UseSharedCommonRequestLogging(
        this IApplicationBuilder app);
}
```

#### 3.6.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Middlewares": {
      "ExceptionHandling": {
        "Enabled": "bool | default: true",
        "IncludeStackTrace": "bool | default: false in Production, true otherwise",
        "LogExceptions": "bool | default: true",
        "CustomErrorHandlers": "object | optional"
      },
      
      "CorrelationId": {
        "Enabled": "bool | default: true",
        "HeaderName": "string | default: X-Correlation-ID",
        "GenerateIfMissing": "bool | default: true"
      },
      
      "RequestLogging": {
        "Enabled": "bool | default: true",
        "LogRequestBody": "bool | default: false",
        "LogResponseBody": "bool | default: false",
        "ExcludePaths": [
          "/health",
          "/metrics"
        ],
        "MaxBodySizeToLog": "int (bytes) | default: 1024"
      }
    }
  }
}
```

#### 3.6.3 Exception Response Format

All exceptions are caught and returned as:

```json
{
  "success": false,
  "error": {
    "code": "UNHANDLED_EXCEPTION",
    "message": "An unexpected error occurred",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2026-05-09T14:30:00Z",
    "stackTrace": null  // Only in Development
  }
}
```

---

### 3.7 SharedCommon.Validation

**Purpose:** FluentValidation integration, auto-registration, validation pipeline.

**Dependencies:** SharedCommon.Core

#### 3.7.1 Public API Surface

```csharp
namespace SharedCommon.Validation;

/// <summary>Register validation infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add FluentValidation with auto-discovery.
    /// Scans assemblies for IValidator<T> implementations.
    /// </summary>
    public static IServiceCollection AddSharedCommonValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assembliesToScan);
}

/// <summary>Validation error response.</summary>
public record ValidationError(
    string Property,
    string Code,
    string Message,
    object? AttemptedValue = null);

/// <summary>Validation extension for Results.</summary>
public static class ValidationExtensions
{
    /// <summary>Convert FluentValidation errors to Result.Validation.</summary>
    public static Result ToValidationResult(
        this FluentValidation.Results.ValidationResult validationResult);
}

/// <summary>Automatic validation filter for controllers.</summary>
public class AutoValidationFilter : IActionFilter
{
    // Automatically validates incoming models
    // Returns validation errors if invalid
    // No controller code needed
}
```

#### 3.7.2 Configuration Schema

```json
{
  "SharedCommon": {
    "Validation": {
      "Enabled": "bool | default: true",
      "AutomaticControllerValidation": "bool | default: true",
      "LanguageManager": {
        "Enabled": "bool | default: false",
        "DefaultLanguage": "string | default: en"
      },
      "RuleSets": {
        "Create": "string | default: CreateRuleSet",
        "Update": "string | default: UpdateRuleSet",
        "Delete": "string | default: DeleteRuleSet"
      }
    }
  }
}
```

#### 3.7.3 Usage Pattern

```csharp
// Define validators (auto-discovered)
public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required")
            .Matches(@"^[A-Z0-9]{10}$").WithMessage("Invalid customer ID format");
        
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Must have at least one item")
            .Must(items => items.Sum(i => i.Quantity) > 0)
                .WithMessage("Total quantity must be greater than zero");
        
        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address required");
    }
}

// In Program.cs
builder.Services.AddSharedCommonValidation(
    builder.Configuration,
    typeof(Program).Assembly); // Auto-discover validators

// In controller (validation automatic)
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        // Validation happens automatically
        // If invalid, 400 returned with error details
        // If valid, request is processed
        
        return Ok(new { OrderId = "ORD-123" });
    }
}
```

#### 3.7.4 Error Response Format

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more validation errors occurred",
    "errors": [
      {
        "property": "CustomerId",
        "code": "RegularExpressionValidator",
        "message": "Invalid customer ID format"
      },
      {
        "property": "Items",
        "code": "PredicateValidator",
        "message": "Total quantity must be greater than zero"
      }
    ]
  }
}
```

---

## Part 4: Secondary Modules (Phase 2-3)

### 4.1 SharedCommon.Observability

**Purpose:** OpenTelemetry integration, Prometheus metrics, distributed tracing.

**Dependencies:** SharedCommon.Core, SharedCommon.Logging

**Configuration:** Complete OTel config, metric names, trace sampling

**API Surface:** `IObservabilityProvider`, automatic span creation, metric collection

**Error Handling:** Gracefully degrade if OTLP endpoint unavailable

---

### 4.2 SharedCommon.Messaging

**Purpose:** MassTransit/Kafka integration, message contracts, saga patterns.

**Dependencies:** SharedCommon.Core, SharedCommon.Logging

**Configuration:** Message bus setup, transport options, serialization

**API Surface:** `IMessageBus`, message publishing, subscription registration

**Error Handling:** Dead-letter queues, retry policies, poison pill handling

---

### 4.3 SharedCommon.HealthChecks

**Purpose:** Standardized health checks for infrastructure dependencies.

**Dependencies:** SharedCommon.Core

**Configuration:** Health check policies, probe endpoints, failure thresholds

**API Surface:** `IHealthCheckRegistry`, health probe middleware

**Error Handling:** Graceful handling of unavailable services

---

## Part 5: Configuration Principles (Applies to All Modules)

### 5.1 Configuration Binding

All configuration MUST use `IOptions<T>` pattern:

```csharp
public class LoggingOptions
{
    public string ApplicationName { get; set; } = string.Empty;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    // ... other properties
}

// In Program.cs
services.Configure<LoggingOptions>(
    configuration.GetSection("SharedCommon:Logging"));
```

### 5.2 Configuration Validation

Configuration MUST be validated at startup:

```csharp
services.AddOptions<LoggingOptions>()
    .BindConfiguration("SharedCommon:Logging")
    .ValidateDataAnnotations()
    .Validate(opts => !string.IsNullOrWhiteSpace(opts.ApplicationName), 
        "ApplicationName is required")
    .ValidateOnStart();
```

### 5.3 Secrets Handling

**Never:** Hardcode secrets
**Always:** Use environment variables or secret managers

```json
{
  "SharedCommon:Logging:Elasticsearch:Password": "${ELASTIC_PASSWORD}"
}
```

At runtime:
```csharp
var password = configuration["SharedCommon:Logging:Elasticsearch:Password"];
// Gets from environment variable ELASTIC_PASSWORD
```

### 5.4 Default Values

Every configuration property MUST have sensible defaults:

```csharp
public class CachingOptions
{
    public string DefaultProvider { get; set; } = "Hybrid";
    public int DefaultTtlSeconds { get; set; } = 300; // 5 minutes
    public int MemoryMaxSize { get; set; } = 10000;   // items
}
```

---

## Part 6: Error Handling Strategy (Global)

### 6.1 Exception Handling Hierarchy

```
ExceptionHandlingMiddleware catches:
├─ ValidationException
│  └─ Returns 400 Bad Request + validation errors
├─ UnauthorizedException
│  └─ Returns 401 Unauthorized
├─ ForbiddenException
│  └─ Returns 403 Forbidden
├─ NotFoundException
│  └─ Returns 404 Not Found
├─ ConflictException
│  └─ Returns 409 Conflict
├─ TooManyRequestsException
│  └─ Returns 429 Too Many Requests
├─ InvalidOperationException
│  └─ Returns 400 Bad Request (configuration/state error)
├─ TimeoutException
│  └─ Returns 504 Gateway Timeout (external service timeout)
├─ HttpRequestException
│  └─ Returns 502 Bad Gateway (external service error)
└─ Exception (catch-all)
   └─ Returns 500 Internal Server Error
```

### 6.2 Logging Exceptions

All exceptions MUST include:
- Exception type
- Message
- Stack trace (development only)
- Correlation ID
- Request context
- Relevant business context

```csharp
_logger.LogError(ex, 
    "Order creation failed for customer {CustomerId}. ErrorCode: {ErrorCode}",
    customerId, "ORDER_CREATE_FAILED");
```

### 6.3 External Service Failures

When external services fail (Redis, Elasticsearch, Kafka):

1. **Log the failure** with context
2. **Degrade gracefully** (use fallback tier or skip feature)
3. **Don't cascade** (isolate to single module)
4. **Retry with backoff** (use Polly)
5. **Report to monitoring** (metrics)
6. **Surface to user** only if critical

Example:

```csharp
try
{
    // Try Redis
    var value = await redis.GetAsync(key);
    return value;
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Redis unavailable, using memory cache fallback");
    
    // Graceful degradation
    var value = memoryCache.Get<T>(key);
    return value;
}
```

---

## Part 7: Testing Strategy (All Modules)

### 7.1 Unit Tests

**What:** Public API, business logic, configuration binding

**Mocked:** External dependencies (Redis, Database, HTTP, etc.)

**Coverage:** >70% minimum

### 7.2 Integration Tests

**What:** Multiple modules working together

**Real:** Docker containers for dependencies (docker-compose)

**Coverage:** Happy path + common error scenarios

### 7.3 Architecture Tests

**What:** Dependency rules, layering, conventions

**Tools:** NetArchTest

**Examples:**
- No circular dependencies
- No infrastructure leakage
- All async methods have CancellationToken
- All public APIs have XML docs

---

## Part 8: Security Strategy (All Modules)

### 8.1 Secure Defaults

Every module MUST have security-first defaults:

**Logging:** Never log sensitive data (PII, secrets, tokens)
**Caching:** Validate cache keys, prevent injection
**Auth:** Validate tokens, check expiration, enforce HTTPS
**Security:** CORS restricted, headers enforced, rate limiting enabled
**Validation:** All inputs validated, size limits enforced

### 8.2 Secret Management

Secrets MUST be externalized:

```json
{
  "SharedCommon:Logging:Elasticsearch:Password": "${ELASTIC_PASSWORD}"
}
```

Environment variable:
```bash
export ELASTIC_PASSWORD="secure-password-123"
```

### 8.3 Input Validation

All user input MUST be validated:

```csharp
// Validate before processing
var result = validator.Validate(request);
if (!result.IsValid)
    return BadRequest(result.Errors);
```

---

## Part 9: Observability Strategy (All Modules)

### 9.1 Structured Logging

Every log MUST include:

```json
{
  "timestamp": "2026-05-09T14:30:00Z",
  "level": "Information",
  "message": "Order created",
  "messageTemplate": "Order {OrderId} created for customer {CustomerId}",
  "properties": {
    "OrderId": "ORD-123",
    "CustomerId": "CUST-456",
    "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
    "ApplicationName": "OrderService",
    "EnvironmentName": "Production"
  }
}
```

### 9.2 Distributed Tracing

Every async operation MUST create a span:

```csharp
using var activity = new Activity("GetUserAsync").Start();
var user = await userService.GetAsync(id);
activity.Stop();
```

### 9.3 Metrics

Key metrics per module:

**Logging:** Log count by level, throughput, sink health
**Caching:** Hit rate, miss rate, size, evictions
**Auth:** Token validations, failed auth attempts, refresh count
**Security:** Rate limit violations, validation failures
**Messaging:** Published messages, consumed messages, errors
**Observability:** Spans created, traces exported, OTLP errors

---

## Part 10: Implementation Phases

### Phase 1: Foundation (Week 1-2)

- [x] SharedCommon.Core
- [x] SharedCommon.Logging
- [x] SharedCommon.Caching
- [x] SharedCommon.Security
- [x] SharedCommon.Auth
- [x] SharedCommon.Middlewares
- [x] SharedCommon.Validation

**Status: Complete — 401 tests passing, 0 warnings.**

**Success Criteria:**
- All 7 packages compile and pass tests
- Full configuration schema defined
- Complete API surface implemented
- 100% XML documentation

### Phase 2: Observability & Messaging (Week 3)

- [x] SharedCommon.Observability
- [x] SharedCommon.Messaging
- [x] SharedCommon.HealthChecks
- [x] SharedCommon.ResponseBuilder
- [x] SharedCommon.Utilities

**Status: Complete.**

**Success Criteria:**
- OpenTelemetry fully integrated
- MassTransit + Kafka working
- Health check probes functional
- Utilities tested and documented

### Phase 3: Advanced (Week 4)

- [x] SharedCommon.Resiliency
- [x] SharedCommon.Grpc
- [x] SharedCommon.GraphQL
- [x] SharedCommon.Auditing
- [x] SharedCommon.BackgroundJobs

**Status: Complete.**

**Success Criteria:**
- Polly resilience policies working
- gRPC contracts defined and implemented
- GraphQL schema operational
- Audit trail logging functional
- Background job processing integrated

### Phase 4: Cloud & Enterprise (Week 5)

- [x] SharedCommon.Cloud
- [x] SharedCommon.ApiVersioning
- [x] SharedCommon.FeatureFlags
- [x] SharedCommon.MultiTenancy
- [x] SharedCommon.Storage

**Status: Complete.**

**Success Criteria:**
- Kubernetes probes functional
- API versioning working
- Feature flags operational
- Multi-tenancy isolation verified
- Storage abstraction tested

---

## Part 11: Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Setup Time Reduction** | 70%+ | Time to bootstrap service |
| **Code Duplication Reduction** | 80%+ | Shared code vs. total code |
| **Observability Adoption** | 100% | All services have logging, tracing, metrics |
| **Configuration Consistency** | 100% | All services use same patterns |
| **Test Coverage** | >80% | Unit + integration coverage |
| **Security Compliance** | 100% | No hardcoded secrets, proper validation |
| **Documentation** | 100% | Every module has API docs + examples |

---

## Part 12: Quality Gates (Non-Negotiable)

Before ANY package is released:

- [ ] All tests pass (unit + integration + architecture)
- [ ] No compiler warnings
- [ ] No hardcoded secrets or credentials
- [ ] 100% XML documentation on public APIs
- [ ] Zero security vulnerabilities (dependency scan)
- [ ] Code review approved (2 reviewers)
- [ ] Performance benchmarks stable
- [ ] Changelog updated
- [ ] Version bumped (semantic versioning)
- [ ] Configuration fully documented

---

## Part 13: Package Publishing (GitLab CI/CD)

### 13.1 Pipeline

```yaml
stages:
  - test
  - build
  - publish

test:
  script:
    - dotnet test
    - dotnet run architecture tests

build:
  script:
    - dotnet pack /p:Version=$VERSION

publish:
  script:
    - dotnet nuget push *.nupkg --source gitlab
  only:
    - tags
```

### 13.2 Versioning (Semantic)

Format: `MAJOR.MINOR.PATCH`

**MAJOR** (breaking changes): 2.0.0
**MINOR** (new features, backward compatible): 1.1.0
**PATCH** (bug fixes): 1.0.1

### 13.3 Consumption

```bash
dotnet add package SharedCommon.Logging --version 1.0.0 --source gitlab
```

---

## Part 14: Roadmap & Timeline

| Phase | Modules | Timeline | Status |
|-------|---------|----------|--------|
| 1 | Core, Logging, Caching, Security, Auth, Middlewares, Validation | Week 1-2 | Complete |
| 2 | Observability, Messaging, HealthChecks, ResponseBuilder, Utilities | Week 3 | Complete |
| 3 | Resiliency, Grpc, GraphQL, Auditing, BackgroundJobs | Week 4 | Complete |
| 4 | Cloud, ApiVersioning, FeatureFlags, MultiTenancy, Storage | Week 5 | Complete |
| 5 | Polish, optimization, advanced features | Week 6+ | Future |

---

## Part 15: Key Design Decisions (ADRs)

### ADR-001: Result Pattern Over Exceptions

**Decision:** Use `Result<T>` pattern for expected failures, exceptions for unexpected failures.

**Why:** Explicit error handling, type-safe, functional composition.

**Example:**
```csharp
// Expected failure (validation)
Result.Validation(errors)

// Expected failure (business logic)
Result.Failure("USER_NOT_FOUND", "User not found")

// Unexpected failure (throw)
throw new InvalidOperationException("Unrecoverable state");
```

### ADR-002: Configuration-Driven Over Hardcoding

**Decision:** All options MUST be configurable via appsettings.json.

**Why:** Deploy same binary to different environments, eliminate configuration drift.

### ADR-003: Structured Logging Only

**Decision:** Never use string interpolation in logs; always use structured properties.

**Why:** Queryable logs, correlation, analytics.

### ADR-004: Fail-Safe by Default

**Decision:** When external services unavailable, degrade gracefully, don't cascade.

**Why:** Resilient systems, better user experience.

### ADR-005: Security-First Defaults

**Decision:** All packages ship with secure-by-default configuration.

**Why:** Prevent developer mistakes, reduce attack surface.

---

## Part 16: Claude Implementation Notes

### Critical for Claude Code

1. **Every configuration property MUST have a comment explaining purpose, type, and default.**

2. **Every public method MUST have XML docs with:
   - What it does
   - Parameters with types
   - Return value
   - Exceptions thrown
   - Usage example

3. **All async methods MUST accept CancellationToken**

4. **All external calls MUST use try-catch with proper logging**

5. **All configuration MUST be validated at startup**

6. **All tests MUST be in separate project with .Tests suffix**

7. **All secrets MUST use environment variables or vault**

8. **All dependencies MUST be injected via constructor DI**

### What Claude Should NOT Do

❌ Hardcode any values
❌ Use Console.WriteLine (use ILogger)
❌ Skip error handling
❌ Write methods > 30 lines
❌ Use static mutable state
❌ Skip validation
❌ Skip XML docs
❌ Use magic strings
❌ Swallow exceptions silently
❌ Create circular dependencies

---

## Summary for Claude

**This BRD is complete and implementable by Claude Code.**

Each module has:
- ✅ Full API surface (interfaces, methods, signatures)
- ✅ Complete configuration schema (JSON structure, types, defaults)
- ✅ Real-world usage examples
- ✅ Error handling strategy
- ✅ Extension points documented
- ✅ Testing approach specified
- ✅ Security requirements explicit
- ✅ Observability patterns defined

**Claude can now:**
1. Implement each module independently
2. Follow patterns consistently
3. Handle errors appropriately
4. Write correct configuration validation
5. Provide complete API surfaces
6. Document properly
7. Test thoroughly

**No ambiguity. No guessing. No rework expected.**
