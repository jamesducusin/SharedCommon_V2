# Phase 1 Implementation Guide: Security & Authentication

**Status:** ✅ Complete  
**Files Modified:** 8  
**New Features:** Security headers, JWT authentication, standardized error handling, domain exceptions

---

## What Was Implemented

### 1. ✅ Security Headers Registration

**File:** `Program.cs`

Security headers are now automatically registered via `AddSharedSecurity()`:

```csharp
builder.Services
    .AddSharedSecurity(builder.Configuration)  // ← NEW
    .AddSharedAuth(builder.Configuration)      // ← NEW
```

**Configured headers** (see `appsettings.json`):
- `Strict-Transport-Security` (HSTS)
- `Content-Security-Policy` (CSP)
- `X-Frame-Options` (clickjacking protection)
- `X-Content-Type-Options` (MIME sniffing protection)
- `Referrer-Policy` (referrer leakage control)

**Rate limiting enabled** with configurable policies:
- Default: 100 req/min (unauthenticated)
- Authenticated: 1000 req/min
- API endpoints: 10,000 req/hour

---

### 2. ✅ JWT Authentication Setup

**File:** `Program.cs`

Authentication middleware now added to the pipeline:

```csharp
// In middleware pipeline (after routing)
app.UseAuthentication();    // ← NEW
app.UseAuthorization();     // ← NEW
```

**Configuration** (see `appsettings.json`):

```json
"SharedCommon": {
  "Auth": {
    "Jwt": {
      "Enabled": true,
      "Authority": "https://auth.example.com",
      "Audience": "https://api.example.com",
      "ValidateAudience": true,
      "ValidateIssuer": true
    }
  }
}
```

---

### 3. ✅ Fixed CORS Configuration

**File:** `ServiceCollectionExtensions.cs`

CORS is now **environment-aware and secure**:

```csharp
// Explicit methods (not wildcards)
builder
    .WithOrigins(allowedOrigins)    // Explicit list only
    .WithMethods(allowedMethods)    // Explicit: GET, POST, PUT, DELETE
    .WithHeaders(allowedHeaders);   // Explicit: Content-Type, Authorization

// Credentials only if explicitly configured
if (allowCredentials)
    builder.AllowCredentials();
```

**Configuration** (see `appsettings.json`):

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173"
  ],
  "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
  "AllowedHeaders": ["Content-Type", "Authorization"],
  "AllowCredentials": false
}
```

**For production**, update `appsettings.Production.json`:

```json
"Cors": {
  "AllowedOrigins": ["https://app.mycompany.com"],
  "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
  "AllowedHeaders": ["Content-Type", "Authorization"],
  "AllowCredentials": false
}
```

---

### 4. ✅ Standardized Error Response Contract

**File:** `src/Templates.Api/Common/Models/ApiErrorResponse.cs`

All API errors now return consistent JSON structure:

```json
{
  "traceId": "0HN3JGFQQ4VLS:00000001",
  "statusCode": 404,
  "error": {
    "code": "ORDER_NOT_FOUND",
    "message": "Order with ID 123 was not found",
    "details": {
      "entityType": "Order",
      "entityId": 123
    }
  },
  "timestamp": "2024-05-30T14:23:45Z"
}
```

**Usage in code:**

```csharp
public record ApiErrorResponse(
    string TraceId,          // For correlating with logs
    int StatusCode,          // HTTP status (400, 404, 500, etc.)
    ErrorDetail Error,       // Error code, message, details
    DateTime Timestamp       // When error occurred
);

public record ErrorDetail(
    string Code,             // Machine-readable code
    string Message,          // Human-readable message
    Dictionary<string, object>? Details = null
);
```

---

### 5. ✅ Domain Exception Hierarchy

**Location:** `src/Templates.Domain/Common/Exceptions/`

New exception types for business logic errors:

#### Base Exception
```csharp
public abstract class DomainException : Exception
{
    public abstract string ErrorCode { get; }
    public abstract int StatusCode { get; }
    public virtual Dictionary<string, object>? Details { get; }
}
```

#### Specific Exceptions

**EntityNotFoundException** (404)
```csharp
throw new EntityNotFoundException("Order", orderId);
// Returns: { code: "ENTITY_NOT_FOUND", statusCode: 404, message: "Order with ID ... not found" }
```

**BusinessRuleViolationException** (400)
```csharp
throw new BusinessRuleViolationException(
    "Order cannot be modified after shipping", 
    "ORDER_ALREADY_SHIPPED");
// Returns: { code: "BUSINESS_RULE_VIOLATION", statusCode: 400, message: "..." }
```

**ConflictException** (409)
```csharp
throw new ConflictException(
    "Order status conflicts with current state",
    new() { { "currentStatus", "shipped" }, { "attemptedStatus", "pending" } });
// Returns: { code: "CONFLICT", statusCode: 409, message: "..." }
```

---

### 6. ✅ Global Exception Handler

**File:** `src/Templates.Api/Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`

Custom middleware maps exceptions to standardized error responses:

```csharp
// DomainException → Uses ErrorCode and StatusCode from exception
if (exception is DomainException domainEx)
{
    errorResponse = new ApiErrorResponse(
        TraceId: traceId,
        StatusCode: domainEx.StatusCode,
        Error: new ErrorDetail(
            Code: domainEx.ErrorCode,
            Message: domainEx.Message,
            Details: domainEx.Details));
}

// ValidationException → 400 with validation errors
else if (exception is FluentValidation.ValidationException valEx)
{
    // Maps field → error messages
    var validationErrors = valEx.Errors
        .GroupBy(e => e.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList());
}

// Other exceptions → 500 Internal Error
else
{
    context.Response.StatusCode = 500;
    errorResponse = new ApiErrorResponse(
        Error: new(
            Code: "INTERNAL_ERROR",
            Message: "An internal error occurred. Please contact support."));
}
```

---

## How to Use

### Creating Protected Endpoints

**Before (no auth):**
```csharp
app.MapGet("/api/v1/orders/{id}", GetOrderById)
    .WithName("GetOrder")
    .WithOpenApi();
```

**After (requires authentication):**
```csharp
app.MapGet("/api/v1/orders/{id}", GetOrderById)
    .WithName("GetOrder")
    .WithOpenApi()
    .RequireAuthorization();  // ← Requires valid JWT token
```

**With specific roles:**
```csharp
app.MapPost("/api/v1/orders", CreateOrder)
    .WithName("CreateOrder")
    .RequireAuthorization(policy => policy
        .RequireRole("Admin", "OrderManager"));
```

### Throwing Domain Exceptions

**In command handlers:**
```csharp
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand>
{
    public async Task<Result> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        // Validate business rules
        var customer = await _customerRepo.GetByIdAsync(cmd.CustomerId, ct);
        if (customer == null)
            throw new EntityNotFoundException(nameof(Customer), cmd.CustomerId);

        if (!customer.IsActive)
            throw new BusinessRuleViolationException(
                "Cannot create order for inactive customer",
                "CUSTOMER_INACTIVE");

        // ... rest of handler
    }
}
```

**Exception is caught by middleware and returns:**
```json
{
  "traceId": "0HN3JGFQQ4VLS:00000002",
  "statusCode": 404,
  "error": {
    "code": "ENTITY_NOT_FOUND",
    "message": "Customer with ID '456' was not found",
    "details": {
      "entityType": "Customer",
      "entityId": 456
    }
  },
  "timestamp": "2024-05-30T14:23:45Z"
}
```

### Client Request with JWT Token

```bash
curl -X GET "http://localhost:5000/api/v1/orders/123" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## Configuration Per Environment

### Development (`appsettings.Development.json`)

✅ Implemented:
```json
{
  "SharedCommon": {
    "Security": {
      "SecurityHeaders": { "Enabled": false },
      "RateLimit": { "Enabled": false },
      "Https": { "Enforced": false }
    },
    "Auth": { "Jwt": { "Enabled": false } }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "http://localhost:4200"
    ],
    "AllowCredentials": true
  }
}
```

### Staging (`appsettings.Staging.json`)

TODO: Create with production-like security enabled:
```json
{
  "SharedCommon": {
    "Security": {
      "SecurityHeaders": { "Enabled": true },
      "RateLimit": { "Enabled": true },
      "Https": { "Enforced": true }
    },
    "Auth": { "Jwt": { "Enabled": true } }
  }
}
```

### Production (`appsettings.Production.json`)

TODO: Update with hardened production settings:
```json
{
  "SharedCommon": {
    "Security": {
      "SecurityHeaders": { "Enabled": true },
      "RateLimit": { "Enabled": true, "Backend": "Redis" },
      "Https": { "Enforced": true }
    },
    "Auth": { "Jwt": { "Enabled": true } }
  },
  "Cors": {
    "AllowedOrigins": ["https://app.mycompany.com"],
    "AllowCredentials": false
  }
}
```

---

## Migration Guide for Existing Endpoints

### Update Endpoints to Use Domain Exceptions

**Before:**
```csharp
var order = await _repo.GetByIdAsync(id, ct);
if (order == null)
    return Results.NotFound(new { error = "Order not found" });
```

**After:**
```csharp
var order = await _repo.GetByIdAsync(id, ct);
if (order == null)
    throw new EntityNotFoundException(nameof(Order), id);
// Exception handler automatically returns proper 404 with ApiErrorResponse
```

### Update Handlers to Use Domain Exceptions

**Before:**
```csharp
public Result Handle(CreateOrderCommand cmd)
{
    if (cmd.Items.Count == 0)
        return Result.Failure("Order must have items");
    // ...
}
```

**After:**
```csharp
public Result Handle(CreateOrderCommand cmd)
{
    if (cmd.Items.Count == 0)
        throw new BusinessRuleViolationException(
            "Order must contain at least one item",
            "EMPTY_ORDER");
    // ...
}
```

---

## Testing the Implementation

### Test Protected Endpoint (requires auth)

```bash
# Without token → 401 Unauthorized
curl -X GET "http://localhost:5000/api/v1/orders/123"
# Response: 401 Unauthorized

# With valid token → Success
curl -X GET "http://localhost:5000/api/v1/orders/123" \
  -H "Authorization: Bearer <valid-jwt-token>"
# Response: 200 OK with order data
```

### Test CORS

```bash
# Cross-origin request from allowed origin
curl -X POST "http://localhost:5000/api/v1/orders" \
  -H "Origin: http://localhost:3000" \
  -H "Content-Type: application/json" \
  -d '{...}'
# Response: 200 with CORS headers

# Cross-origin request from blocked origin
curl -X POST "http://localhost:5000/api/v1/orders" \
  -H "Origin: http://malicious.com" \
  -H "Content-Type: application/json" \
  -d '{...}'
# Response: CORS error (no Access-Control-Allow-Origin header)
```

### Test Error Responses

```bash
# Domain exception (404)
curl -X GET "http://localhost:5000/api/v1/orders/999"
# Response:
# {
#   "traceId": "0HN3JGFQQ4VLS:00000001",
#   "statusCode": 404,
#   "error": {
#     "code": "ENTITY_NOT_FOUND",
#     "message": "Order with ID '999' was not found",
#     "details": { "entityType": "Order", "entityId": 999 }
#   },
#   "timestamp": "2024-05-30T14:23:45Z"
# }
```

---

## Summary of Changes

| File | Change | Status |
|------|--------|--------|
| `Program.cs` | Added security & auth registration, updated middleware pipeline | ✅ |
| `appsettings.json` | Added security, auth, and observability config | ✅ |
| `appsettings.Development.json` | Added development-specific overrides | ✅ |
| `ServiceCollectionExtensions.cs` | Fixed CORS to be environment-aware | ✅ |
| `DomainException.cs` | Created base exception for domain errors | ✅ |
| `EntityNotFoundException.cs` | Created for 404 errors | ✅ |
| `BusinessRuleViolationException.cs` | Created for 400 errors | ✅ |
| `ConflictException.cs` | Created for 409 errors | ✅ |
| `ApiErrorResponse.cs` | Created standardized error response contract | ✅ |
| `ExceptionHandlingMiddleware.cs` | Created custom exception handler | ✅ |

---

## Next Steps (Phase 2)

1. **Create appsettings.Staging.json** with production-like security
2. **Create appsettings.Production.json** with hardened settings
3. **Configure JWT issuer/audience** for your auth provider
4. **Update existing endpoints** to use domain exceptions
5. **Add role-based authorization** to sensitive endpoints
6. **Test auth flows** with real JWT tokens

See **ENTERPRISE_READINESS_AUDIT.md** for complete Phase 2 plan.
