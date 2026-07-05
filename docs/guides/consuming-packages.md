# Consuming SharedCommon Packages

This guide is for developers who want to install SharedCommon packages into their own .NET projects.

---

## Prerequisites

- .NET 8 SDK or later
- ASP.NET Core project (Web API, minimal API, or Worker Service)
- A NuGet feed that hosts the SharedCommon packages (your organization's private feed, or a local build)

---

## Installing Packages

Each SharedCommon package is independent. Install only what you need.

```bash
# Foundation — always install this first
dotnet add package SharedCommon.Core

# Add whichever packages apply to your service
dotnet add package SharedCommon.Logging
dotnet add package SharedCommon.Observability
dotnet add package SharedCommon.Caching
dotnet add package SharedCommon.Auth
dotnet add package SharedCommon.Security
dotnet add package SharedCommon.Validation
dotnet add package SharedCommon.Middlewares
dotnet add package SharedCommon.ResponseBuilder
dotnet add package SharedCommon.HealthChecks
dotnet add package SharedCommon.Resiliency
dotnet add package SharedCommon.Messaging        # RabbitMQ or Kafka — transport is config-driven
dotnet add package SharedCommon.Grpc
dotnet add package SharedCommon.GraphQL          # Hot Chocolate GraphQL
dotnet add package SharedCommon.Auditing         # Structured audit trail
dotnet add package SharedCommon.BackgroundJobs   # Hangfire job scheduling
dotnet add package SharedCommon.Cloud            # Blob storage, secrets, cloud queues
dotnet add package SharedCommon.ApiVersioning    # URL-segment / header versioning
dotnet add package SharedCommon.FeatureFlags     # Feature flags via Microsoft.FeatureManagement
dotnet add package SharedCommon.MultiTenancy     # Header/claim/subdomain tenant resolution
dotnet add package SharedCommon.Storage          # Provider-agnostic file storage (local or cloud)
dotnet add package SharedCommon.Utilities
```

> All packages target **net8.0** and use **Central Package Management** internally. Your project can target net8.0 or later.

---

## Typical Program.cs Setup

Below is a complete `Program.cs` for a standard REST microservice. Pick the lines that apply to your service.

```csharp
using SharedCommon.Core;
using SharedCommon.Logging;
using SharedCommon.Observability;
using SharedCommon.Caching;
using SharedCommon.Auth;
using SharedCommon.Security;
using SharedCommon.Validation;
using SharedCommon.Middlewares;
using SharedCommon.ResponseBuilder;
using SharedCommon.HealthChecks;
using SharedCommon.Resiliency;
using SharedCommon.Messaging;
using SharedCommon.MultiTenancy;

var builder = WebApplication.CreateBuilder(args);

// ── Foundation (required by all other packages) ──────────────────────────────
builder.Services.AddSharedCommonCore(builder.Configuration);

// ── Structured logging (Serilog) ─────────────────────────────────────────────
builder.Services.AddSharedCommonLogging(builder.Configuration);

// ── OpenTelemetry tracing + metrics ──────────────────────────────────────────
builder.Services.AddSharedObservability(builder.Configuration);

// ── Hybrid cache (memory + optional Redis) ───────────────────────────────────
builder.Services.AddSharedCaching(builder.Configuration);

// ── JWT authentication + ICurrentUser ────────────────────────────────────────
builder.Services.AddSharedCommonAuth(builder.Configuration);

// ── Security headers, rate limiting, CORS ────────────────────────────────────
builder.Services.AddSharedSecurity(builder.Configuration);

// ── FluentValidation auto-discovery ──────────────────────────────────────────
builder.Services.AddSharedCommonValidation(builder.Configuration, typeof(Program).Assembly);

// ── Standardized API response builder ────────────────────────────────────────
builder.Services.AddSharedResponseBuilder();

// ── Liveness + readiness health checks ───────────────────────────────────────
builder.Services.AddSharedHealthChecks(builder.Configuration);

// ── Polly resilience pipelines ────────────────────────────────────────────────
builder.Services.AddSharedResiliency(builder.Configuration);

// ── Multi-tenancy tenant resolution (header, claim, subdomain, query-string) ─
builder.Services.AddSharedMultiTenancy(builder.Configuration);

// ── MassTransit + RabbitMQ messaging ─────────────────────────────────────────
builder.Services.AddSharedMessaging(builder.Configuration, bus =>
{
    bus.AddConsumer<MyOrderCreatedConsumer>();
});

// ── Middleware options ────────────────────────────────────────────────────────
builder.Services.AddSharedCommonMiddlewares(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware pipeline (order matters) ──────────────────────────────────────
app.UseSharedCommonExceptionHandling();   // Must be first
app.UseSharedCommonCorrelationId();
app.UseSharedCommonRequestLogging();

// ── Multi-tenancy middleware (before authentication) ──────────────────────────
app.UseSharedMultiTenancy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseSharedHealthEndpoints();           // Maps /health/live and /health/ready

app.Run();
```

> **MultiTenancy Security:** This package resolves tenants but does NOT enforce data isolation.
> Your application MUST filter all queries by tenant, include tenant ID in cache keys, validate cross-tenant access,
> and capture tenant ID as a string value (not reference) in background jobs.
> See [Security Guidelines](../standards/security-guidelines.md#multi-tenancy-data-isolation) for detailed patterns.

---

## appsettings.json Reference

All SharedCommon configuration lives under the `"SharedCommon"` key. Every property has a sensible default — only override what differs from the default.

```json
{
  "SharedCommon": {
    "Core": {
      "ApplicationName": "OrderService",
      "EnvironmentName": "Production",
      "Version": "1.0.0"
    },
    "Logging": {
      "ApplicationName": "OrderService",
      "MinimumLevel": "Information",
      "Console": { "Enabled": true, "Theme": "Colored" },
      "File": {
        "Enabled": true,
        "Path": "./logs/order-service-.txt",
        "RollingInterval": "Day"
      },
      "Elasticsearch": {
        "Enabled": false,
        "Url": "http://elasticsearch:9200"
      }
    },
    "Observability": {
      "ServiceName": "OrderService",
      "ServiceVersion": "1.0.0",
      "OtlpEndpoint": "http://otel-collector:4317",
      "SamplingRatio": 1.0
    },
    "Caching": {
      "DefaultProvider": "Hybrid",
      "DefaultTtlSeconds": 300,
      "Redis": {
        "Enabled": true,
        "Connection": "redis:6379",
        "KeyPrefix": "orders:"
      }
    },
    "Auth": {
      "Jwt": {
        "Issuer": "https://auth.example.com",
        "Audience": "https://api.example.com",
        "ExpirationMinutes": 60
      }
    },
    "Security": {
      "SecurityHeaders": { "Enabled": true },
      "RateLimit": {
        "Enabled": true,
        "Policies": {
          "Default": { "MaxRequests": 100, "WindowSeconds": 60 }
        }
      },
      "Cors": {
        "AllowedOrigins": ["https://app.example.com"]
      }
    },
    "Validation": {
      "AutomaticControllerValidation": true
    },
    "HealthChecks": {
      "DefaultTimeout": "00:00:05",
      "Redis": { "Enabled": true, "Name": "redis" },
      "ExternalHttp": [
        { "Name": "payment-api", "Uri": "https://payment.internal/health" }
      ]
    },
    "Resiliency": {
      "Retry": {
        "MaxAttempts": 3,
        "BaseDelay": "00:00:00.5",
        "MaxDelay": "00:00:30"
      },
      "CircuitBreaker": {
        "FailureRatio": 0.5,
        "MinimumThroughput": 5,
        "BreakDuration": "00:00:30"
      },
      "Timeout": {
        "Duration": "00:00:30"
      }
    },
    "Messaging": {
      "Transport": "RabbitMQ",
      "RabbitMQ": {
        "Host": "rabbitmq",
        "Port": 5672,
        "VirtualHost": "/",
        "Username": "guest"
      },
      "Retry": {
        "MaxAttempts": 3,
        "MinInterval": "00:00:01",
        "MaxInterval": "00:00:30"
      }
    },
    "Auditing": {
      "Backend": "Logging",
      "CaptureValueSnapshots": true,
      "RetentionDays": 90
    },
    "BackgroundJobs": {
      "Backend": "InMemory",
      "WorkerCount": 5,
      "EnableDashboard": false
    },
    "Middlewares": {
      "ExceptionHandling": { "IncludeStackTrace": false },
      "CorrelationId": { "HeaderName": "X-Correlation-ID" },
      "RequestLogging": { "ExcludePaths": ["/health", "/metrics"] }
    },
    "Grpc": {
      "EnableReflection": false,
      "EnableHealthCheck": true
    },
    "Cloud": {
      "Provider": "Azure",
      "Azure": {
        "StorageAccountName": "mystorageaccount",
        "KeyVaultUri": "https://my-vault.vault.azure.net/",
        "ServiceBusNamespace": "my-namespace.servicebus.windows.net",
        "UseManagedIdentity": true
      }
    },
    "ApiVersioning": {
      "DefaultVersion": "1.0",
      "AssumeDefaultWhenUnspecified": true,
      "ReportApiVersions": true,
      "Strategy": {
        "UrlSegment": true
      }
    },
    "FeatureFlags": {
      "CacheTtlSeconds": 0,
      "LogEvaluations": true
    },
    "MultiTenancy": {
      "Enabled": true,
      "Strategy": "Header",
      "HeaderName": "X-Tenant-Id",
      "ClaimName": "tenant_id",
      "QueryStringKey": "tenantId",
      "RequireTenant": false
    },
    "Storage": {
      "Provider": "Local",
      "LocalBasePath": "./storage",
      "ContainerName": "uploads"
    }
  },
  "FeatureManagement": {
    "NewCheckoutFlow": true,
    "BetaDashboard": false
  }
}
```

> **Secrets** — Never put `Auth:Jwt:SecretKey`, Redis passwords, RabbitMQ passwords, or cloud credentials in `appsettings.json`. Use .NET User Secrets locally and your secrets manager (Azure Key Vault, AWS Secrets Manager, Kubernetes Secrets) in production.

---

## Package Dependency Map

Some packages depend on others. Install the dependencies when the table says so.

| Package | Depends On |
|---------|-----------|
| SharedCommon.Core | — (no dependencies) |
| SharedCommon.Logging | Core |
| SharedCommon.Observability | Core |
| SharedCommon.Caching | Core |
| SharedCommon.Auth | Core |
| SharedCommon.Security | Core |
| SharedCommon.Validation | Core |
| SharedCommon.Middlewares | Core |
| SharedCommon.ResponseBuilder | Core |
| SharedCommon.HealthChecks | Core |
| SharedCommon.Resiliency | Core |
| SharedCommon.Messaging | Core |
| SharedCommon.Grpc | Core |
| SharedCommon.GraphQL | Core |
| SharedCommon.Auditing | Core |
| SharedCommon.BackgroundJobs | Core |
| SharedCommon.Cloud | Core |
| SharedCommon.ApiVersioning | Core |
| SharedCommon.FeatureFlags | Core |
| SharedCommon.MultiTenancy | Core |
| SharedCommon.Storage | Core |
| SharedCommon.Utilities | — (no dependencies) |

All packages resolve their own transitive NuGet dependencies automatically.

---

## The Result Pattern

Every SharedCommon service method returns `Result<T>` instead of throwing exceptions for expected failures. This is the core pattern you will interact with most.

```csharp
// Result<T> has three cases:
var result = await cacheService.GetOrSetAsync("key", factory, ct: ct);

// Pattern-match to handle all outcomes:
return result switch
{
    Result<MyData>.Success s  => Ok(s.Data),
    Result<MyData>.Validation v => UnprocessableEntity(v.Errors),
    Result<MyData>.Failure f  => Problem(f.Message, statusCode: 500),
    _ => StatusCode(500)
};

// Or use IResponseBuilder to map automatically:
return _responseBuilder.FromResult(result);
```

---

## Secrets Management

Use .NET User Secrets during local development:

```bash
dotnet user-secrets init
dotnet user-secrets set "SharedCommon:Auth:Jwt:SecretKey" "your-32-char-minimum-secret-key"
dotnet user-secrets set "SharedCommon:Messaging:Password" "rabbitmq-password"
dotnet user-secrets set "SharedCommon:Caching:Redis:Connection" "localhost:6379"
```

---

## Startup Validation

All packages call `.ValidateOnStart()` on their options. If required configuration is missing or invalid, the application **fails fast at startup** with a clear error message rather than failing at runtime. This means:

- Misconfigured secrets → startup error, not a runtime 500
- Missing required fields → startup error with the field name
- Out-of-range values → startup error with the constraint

---

## Individual Package Guides

Each package has its own README with detailed configuration and usage:

| Package | README |
|---------|--------|
| SharedCommon.Core | [src/SharedCommon.Core/README.md](../../src/SharedCommon.Core/README.md) |
| SharedCommon.Logging | [src/SharedCommon.Logging/README.md](../../src/SharedCommon.Logging/README.md) |
| SharedCommon.Observability | [src/SharedCommon.Observability/README.md](../../src/SharedCommon.Observability/README.md) |
| SharedCommon.Caching | [src/SharedCommon.Caching/README.md](../../src/SharedCommon.Caching/README.md) |
| SharedCommon.Auth | [src/SharedCommon.Auth/README.md](../../src/SharedCommon.Auth/README.md) |
| SharedCommon.Security | [src/SharedCommon.Security/README.md](../../src/SharedCommon.Security/README.md) |
| SharedCommon.Validation | [src/SharedCommon.Validation/README.md](../../src/SharedCommon.Validation/README.md) |
| SharedCommon.Middlewares | [src/SharedCommon.Middlewares/README.md](../../src/SharedCommon.Middlewares/README.md) |
| SharedCommon.ResponseBuilder | [src/SharedCommon.ResponseBuilder/README.md](../../src/SharedCommon.ResponseBuilder/README.md) |
| SharedCommon.HealthChecks | [src/SharedCommon.HealthChecks/README.md](../../src/SharedCommon.HealthChecks/README.md) |
| SharedCommon.Resiliency | [src/SharedCommon.Resiliency/README.md](../../src/SharedCommon.Resiliency/README.md) |
| SharedCommon.Messaging | [src/SharedCommon.Messaging/README.md](../../src/SharedCommon.Messaging/README.md) |
| SharedCommon.Grpc | [src/SharedCommon.Grpc/README.md](../../src/SharedCommon.Grpc/README.md) |
| SharedCommon.GraphQL | [src/SharedCommon.GraphQL/README.md](../../src/SharedCommon.GraphQL/README.md) |
| SharedCommon.Auditing | [src/SharedCommon.Auditing/README.md](../../src/SharedCommon.Auditing/README.md) |
| SharedCommon.BackgroundJobs | [src/SharedCommon.BackgroundJobs/README.md](../../src/SharedCommon.BackgroundJobs/README.md) |
| SharedCommon.Cloud | [src/SharedCommon.Cloud/README.md](../../src/SharedCommon.Cloud/README.md) |
| SharedCommon.ApiVersioning | [src/SharedCommon.ApiVersioning/README.md](../../src/SharedCommon.ApiVersioning/README.md) |
| SharedCommon.FeatureFlags | [src/SharedCommon.FeatureFlags/README.md](../../src/SharedCommon.FeatureFlags/README.md) |
| SharedCommon.MultiTenancy | [src/SharedCommon.MultiTenancy/README.md](../../src/SharedCommon.MultiTenancy/README.md) |
| SharedCommon.Storage | [src/SharedCommon.Storage/README.md](../../src/SharedCommon.Storage/README.md) |
| SharedCommon.Utilities | [src/SharedCommon.Utilities/README.md](../../src/SharedCommon.Utilities/README.md) |
