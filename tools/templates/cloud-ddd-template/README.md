# Cloud-Ready DDD Project Template

Enterprise-grade project template combining Clean Architecture, Domain-Driven Design, and Vertical Slice Architecture.

**Scaffolding is not yet implemented** — this template serves as a reference for proper structure, best practices, and integration with Cerberus packages.

## What's Included

### Architecture
- **Clean Architecture** — Layered separation: Domain → Application → Infrastructure → Presentation
- **Domain-Driven Design** — Entities, Value Objects, Aggregates, Domain Services
- **Vertical Slice** — Features organized as independent, cohesive slices
- **SOLID Principles** — Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion

### Essential Cerberus Integration
- **SharedCommon.Core** — `Result<T>`, `Guard`, domain exceptions
- **SharedCommon.Logging** — Structured logging via Serilog
- **SharedCommon.Middlewares** — CorrelationId, Global exception handling, request logging
- **SharedCommon.ResponseBuilder** — Standardized `ApiResponse<T>` envelopes
- **SharedCommon.HealthChecks** — Service health checks (liveness, readiness)
- **SharedCommon.Validation** — FluentValidation with DI
- **SharedCommon.Observability** — OpenTelemetry tracing and metrics

### Optional Cerberus Packages (Config-Driven)
- **SharedCommon.Caching** — In-memory + Redis hybrid caching
- **SharedCommon.Messaging** — RabbitMQ or Kafka (switchable)
- **SharedCommon.Cloud** — Azure/AWS blob storage, secrets, queues
- **SharedCommon.MultiTenancy** — Tenant isolation (if SaaS model)
- **SharedCommon.Auditing** — Audit trail (Logging/Database/Messaging backend)
- **SharedCommon.BackgroundJobs** — Hangfire-backed background jobs

### Project Structure

```
[ProjectName]/
├── src/
│   ├── [ProjectName].Api/                  # ASP.NET Core entry point
│   │   ├── Features/
│   │   │   └── Orders/                     # Vertical slice example
│   │   │       ├── Create/                 # Slice: Create order
│   │   │       │   ├── CreateOrderCommand.cs
│   │   │       │   ├── CreateOrderCommandHandler.cs
│   │   │       │   ├── CreateOrderValidator.cs
│   │   │       │   ├── CreateOrderEndpoint.cs
│   │   │       │   └── CreateOrderTests.cs
│   │   │       ├── Get/                    # Slice: Retrieve order
│   │   │       ├── List/                   # Slice: List orders
│   │   │       ├── Update/                 # Slice: Update order
│   │   │       └── Delete/                 # Slice: Delete order
│   │   ├── Infrastructure/
│   │   │   ├── DependencyInjection.cs      # Service registration
│   │   │   ├── Middleware/                 # Custom pipeline middleware
│   │   │   └── Configuration/              # App settings helpers
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.{Environment}.json
│   ├── [ProjectName].Application/         # Use case layer
│   │   ├── Features/
│   │   │   └── Orders/
│   │   │       └── Create/
│   │   │           ├── CreateOrderRequest.cs
│   │   │           ├── CreateOrderResponse.cs
│   │   │           └── ICreateOrderService.cs
│   │   ├── Common/
│   │   │   ├── Behaviors/                  # MediatR pipeline behaviors
│   │   │   ├── Interfaces/                 # Service interfaces
│   │   │   └── Models/                     # Common DTOs
│   │   └── ServiceCollectionExtensions.cs  # App layer DI
│   ├── [ProjectName].Domain/               # Core business logic
│   │   ├── Entities/
│   │   │   └── Order.cs
│   │   ├── ValueObjects/
│   │   │   └── OrderId.cs
│   │   ├── Interfaces/
│   │   │   ├── IOrderRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Events/
│   │   │   └── OrderCreatedDomainEvent.cs
│   │   └── Exceptions/
│   │       └── OrderDomainException.cs
│   └── [ProjectName].Infrastructure/      # External integrations
│       ├── Persistence/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Repositories/
│       │   │   └── OrderRepository.cs
│       │   └── Migrations/
│       ├── ExternalServices/
│       │   ├── PaymentService.cs
│       │   └── NotificationService.cs
│       ├── BackgroundJobs/                 # Optional: Hangfire
│       │   └── SendOrderConfirmationJob.cs
│       └── ServiceCollectionExtensions.cs  # Infra DI
└── tests/
    ├── [ProjectName].UnitTests/
    │   ├── Domain/
    │   │   └── Entities/
    │   │       └── OrderTests.cs
    │   ├── Application/
    │   │   └── Features/
    │   │       └── Orders/
    │   │           └── CreateOrderTests.cs
    │   └── Infrastructure/
    │       └── Repositories/
    │           └── OrderRepositoryTests.cs
    └── [ProjectName].IntegrationTests/
        ├── Features/
        │   └── Orders/
        │       └── CreateOrderTests.cs
        └── WebApplicationFactory.cs
```

## Getting Started

### Prerequisites
- .NET 8.0+
- SQL Server, PostgreSQL, or SQLite (configurable)
- (Optional) Redis for caching
- (Optional) RabbitMQ or Kafka for messaging

### Setup

1. **Clone/use this template:**
   ```bash
   dotnet new install ./tools/templates/cloud-ddd-template
   dotnet new cloud-ddd --name MyProject
   cd MyProject
   ```

2. **Install dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure appsettings:**
   ```json
   {
     "Logging": {
       "LogLevel": { "Default": "Information" }
     },
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=myproject;User=sa;Password=..."
     }
   }
   ```

4. **Run migrations (if using EF Core):**
   ```bash
   dotnet ef database update --project src/[ProjectName].Infrastructure
   ```

5. **Start the API:**
   ```bash
   dotnet run --project src/[ProjectName].Api
   ```

6. **Verify health:**
   ```bash
   curl http://localhost:5000/health
   ```

## Architecture Overview

### Domain Layer (Innermost)
- **Entities**: Aggregate roots with identity (`Order`, `Customer`)
- **Value Objects**: Immutable, equality-based (`OrderId`, `Money`)
- **Domain Events**: Pure domain state changes (`OrderCreatedDomainEvent`)
- **Domain Exceptions**: Business rule violations
- **Specifications**: Complex query logic (if using Specification pattern)
- **No Dependencies**: Except .NET base classes

### Application Layer
- **Commands/Queries**: Request models (MediatR)
- **Handlers**: Use case logic, orchestration
- **Validators**: FluentValidation per request
- **DTOs**: Data transfer models (Request/Response)
- **Interfaces**: Repository, Unit of Work abstractions
- **Depends On**: Domain layer only

### Infrastructure Layer
- **Repositories**: EF Core implementations of domain interfaces
- **Database Context**: Entity mappings, migrations
- **External Services**: Payment, email, third-party APIs
- **Background Jobs**: Hangfire job implementations
- **Cache Stores**: Redis connection, cache logic
- **Depends On**: Application and Domain layers

### Presentation Layer (API)
- **Endpoints**: Minimal APIs or controllers
- **Feature Slices**: Organized by business capability
- **Middleware**: CorrelationId, exception handling, logging
- **Configuration**: DI setup, pipeline configuration
- **Depends On**: All lower layers

## Vertical Slice Example: Orders Feature

Each feature is **self-contained** and can be developed/tested independently.

### Create Order Slice Structure

```
Features/Orders/Create/
├── CreateOrderCommand.cs          # Request model
├── CreateOrderCommandHandler.cs   # Business logic
├── CreateOrderValidator.cs        # Input validation
├── CreateOrderEndpoint.cs         # HTTP endpoint
├── CreateOrderResponse.cs         # Response model
└── CreateOrderTests.cs            # Tests (in test project)
```

**CreateOrderCommand.cs**
```csharp
namespace MyProject.Api.Features.Orders.Create;

public record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemRequest> Items) : IRequest<Result<CreateOrderResponse>>;

public record CreateOrderItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateOrderResponse(Guid OrderId, decimal TotalAmount, DateTime CreatedAt);
```

**CreateOrderCommandHandler.cs**
```csharp
namespace MyProject.Api.Features.Orders.Create;

public sealed class CreateOrderCommandHandler(
    IOrderRepository _orderRepository,
    IUnitOfWork _unitOfWork,
    ILogger<CreateOrderCommandHandler> _logger) : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Validate (Guard is from SharedCommon.Core)
        Guard.AgainstEmptyGuid(request.CustomerId, nameof(request.CustomerId));
        Guard.AgainstEmpty(request.Items, nameof(request.Items));

        // Create aggregate
        var order = Order.Create(request.CustomerId, request.Items
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.UnitPrice))
            .ToList());

        // Persist
        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);

        return Result.Success(new CreateOrderResponse(
            order.Id,
            order.TotalAmount,
            order.CreatedAt));
    }
}
```

**CreateOrderEndpoint.cs**
```csharp
namespace MyProject.Api.Features.Orders.Create;

public static class CreateOrderEndpoint
{
    public static void MapCreateOrder(this WebApplication app)
    {
        app.MapPost("/orders", Handle)
            .WithName("CreateOrder")
            .WithOpenApi()
            .WithSummary("Create a new order")
            .Produces<ApiResponse<CreateOrderResponse>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        CreateOrderCommand command,
        ISender sender,
        IResponseBuilder responseBuilder,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return responseBuilder.From(result)
            .WithStatusCode(StatusCodes.Status201Created)
            .Build();
    }
}
```

## Configuration Examples

### Appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyProjectDb;Trusted_Connection=true;"
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": { "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}" }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/myproject-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  },
  "Caching": {
    "Enabled": true,
    "L1": {
      "MaxSizeMb": 100,
      "TtlSeconds": 300
    },
    "L2": {
      "ConnectionString": "localhost:6379",
      "TtlSeconds": 3600
    }
  },
  "Messaging": {
    "Transport": "RabbitMQ",
    "RabbitMQ": {
      "Host": "localhost",
      "Port": 5672,
      "Username": "guest",
      "Password": "guest"
    }
  }
}
```

### Program.cs Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Cerberus shared services
builder.Services
    .AddSharedLogging()
    .AddSharedObservability()
    .AddSharedHealthChecks()
    .AddSharedValidation()
    .AddSharedResponseBuilder()
    .AddSharedCaching(builder.Configuration)
    .AddSharedMessaging(builder.Configuration);

// Add application services
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

// Build
var app = builder.Build();

// Middleware
app.UseSharedExceptionHandler()
    .UseSharedCorrelationId()
    .UseSharedRequestLogging();

// Health checks
app.MapSharedHealthChecks();

// Feature endpoints
app.MapOrderFeatures();
app.MapProductFeatures();

app.Run();
```

## Testing Strategy

### Unit Tests (Domain & Application)
- **No database**: Use in-memory repositories or mocks
- **Focus**: Business logic, validation, decision trees
- **Framework**: xUnit + Moq + FluentAssertions

```csharp
public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesOrderAndReturnsSuccess()
    {
        // Arrange
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<CreateOrderItemRequest> { new(Guid.NewGuid(), 2, 99.99m) });
        
        var mockRepository = new Mock<IOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateOrderCommandHandler(mockRepository.Object, mockUnitOfWork.Object, Mock.Of<ILogger<CreateOrderCommandHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Tests
- **Database**: SQLite in-memory or test database
- **Focus**: End-to-end flows, middleware, database interactions
- **Framework**: xUnit + `WebApplicationFactory<Program>`

```csharp
public class CreateOrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Post_CreateOrder_WithValidRequest_Returns201()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateOrderCommand(Guid.NewGuid(), new List<CreateOrderItemRequest> { ... });

        // Act
        var response = await client.PostAsJsonAsync("/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsAsync<ApiResponse<CreateOrderResponse>>();
        content.Success.Should().BeTrue();
    }
}
```

## Cerberus Package Integration Points

### When to Use Each Package

| Package | When to Use | Example |
|---------|-----------|---------|
| Core | Always | `Result<T>`, domain exceptions |
| Logging | Always | Structured logging in all layers |
| Middlewares | Always | CorrelationId, exception handling |
| ResponseBuilder | Always | Standardized HTTP responses |
| HealthChecks | Always | `/health`, `/health/live`, `/health/ready` |
| Validation | Most features | Input validation, business rules |
| Observability | Always | Tracing, metrics collection |
| Caching | Performance-critical | Frequently accessed data |
| Messaging | Async workflows | Event publishing, async commands |
| Cloud | Cloud deployment | Blob storage, secrets, queues |
| MultiTenancy | SaaS models only | Tenant isolation, data segregation |
| Auditing | Compliance required | Audit trail, regulatory |
| BackgroundJobs | Long-running tasks | Email, reports, cleanup |

## Best Practices Enforced

1. **No Leaky Abstractions** — Infrastructure implementations hidden behind interfaces
2. **Dependency Injection** — All dependencies resolved from container, never service locator
3. **Async/Await** — All I/O operations async with `CancellationToken`
4. **Structured Logging** — Correlation IDs, request context, no PII
5. **Result Pattern** — Expected failures as `Result<T>`, exceptions for unexpected
6. **No Circular Dependencies** — Enforced by project structure
7. **XML Documentation** — All public APIs documented
8. **Unit Testing** — All business logic testable, mockable
9. **Configuration-Driven** — No hardcoded values, all via `IOptions<T>`
10. **SOLID Principles** — Single Responsibility, Open/Closed, Liskov, Interface Segregation, DI

## Common Tasks

### Add a New Feature

1. Create feature folder: `Features/[DomainEntity]/[Operation]/`
2. Add Command/Handler/Validator/Endpoint
3. Register in `Program.cs` or feature module
4. Add integration tests
5. Update OpenAPI docs

### Add Caching

```csharp
// In handler
var cacheKey = $"order:{orderId}";
var cached = await _cacheService.GetAsync<Order>(cacheKey, ct);
if (cached is not null)
    return Result.Success(cached);

var order = await _repository.GetByIdAsync(orderId, ct);
await _cacheService.SetAsync(cacheKey, order, TimeSpan.FromHours(1), ct);
return Result.Success(order);
```

### Add Background Job

```csharp
// In handler
await _backgroundJobService.ScheduleAsync(
    () => SendOrderConfirmationJob.ExecuteAsync(order.Id),
    TimeSpan.FromMinutes(5),
    ct);
```

### Add Audit Trail

```csharp
// In handler
await _auditService.RecordAsync(
    AuditEntry.Create()
        .ForEntity(nameof(Order), order.Id)
        .WithAction(AuditAction.Created)
        .FromContext(_requestContext)
        .WithMetadata("source", "api"),
    ct);
```

## Security Considerations

- ✅ All secrets in `appsettings.{Environment}.json` (gitignored)
- ✅ No hardcoded connection strings, API keys, or credentials
- ✅ Input validation on all endpoints
- ✅ Authorization checks via policies (not roles)
- ✅ Structured logging excludes PII
- ✅ CORS configured by environment
- ✅ Rate limiting on public endpoints (if using middleware)
- ✅ SQL injection prevented via ORM parameterization
- ✅ XSS protection via Content-Type headers

## Performance Optimization

- **Caching**: L1 (in-memory) + L2 (Redis) hybrid strategy
- **Async/Await**: Non-blocking I/O throughout
- **Pagination**: All list operations paginated
- **Batch Operations**: Bulk inserts/updates where applicable
- **Lazy Loading**: EF Core lazy loading for navigation properties (or use eager)
- **Query Optimization**: Projection to DTOs, indices on frequently queried columns
- **Connection Pooling**: Automatic via EF Core

## Monitoring & Observability

- **Structured Logs**: JSON format, searchable in ELK/Loki
- **Distributed Tracing**: OpenTelemetry ActivitySource per feature
- **Metrics**: Counter for business events, Histogram for latency
- **Health Checks**: Liveness (app running), readiness (ready to accept requests)
- **Correlation IDs**: All requests traced end-to-end

## Troubleshooting

### Application won't start
1. Check connection string in `appsettings.{Environment}.json`
2. Verify database is accessible
3. Check logs for missing NuGet packages
4. Run `dotnet restore` and `dotnet build`

### Tests fail
1. Ensure test database is accessible
2. Check appsettings in test project
3. Verify mocks are configured correctly
4. Run `dotnet test` with verbose output

### Slow API responses
1. Check query performance in database
2. Enable L1 caching for frequently accessed data
3. Use pagination on list endpoints
4. Profile with Application Insights or similar

## References

- [Cerberus Documentation](../../../docs/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://domainlanguage.com/ddd/)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)

---

**Template Version**: 1.0  
**Last Updated**: 2026-05-29  
**Compatible With**: Cerberus v3.0+, .NET 8.0+
