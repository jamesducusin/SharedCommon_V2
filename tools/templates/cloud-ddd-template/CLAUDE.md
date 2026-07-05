# Cloud-Ready DDD Project Template — Architecture Guidelines

**Status**: Reference template for new projects  
**Last Updated**: 2026-05-29

## Template Purpose

This template provides a production-ready project structure combining:
- **Clean Architecture** (layered separation of concerns)
- **Domain-Driven Design** (focus on domain logic)
- **Vertical Slice Architecture** (features as cohesive units)
- **Cloud-Ready** (12-factor app, configurable backends)
- **Kafka-Ready** (optional async messaging)
- **SOLID Principles** (maintainability and testability)

## Architecture Layers

### Layer 1: Domain (Innermost — No Dependencies)

**Responsibility**: Pure business logic, rules, policies

**Contains**:
- `Entities/` — Aggregate roots with identity (e.g., `Order`, `Customer`)
  - Always implement value-based equality (`IEquatable<T>`)
  - Private setters; mutations via methods
  - Raise domain events on state change
- `ValueObjects/` — Immutable, equality-based objects (e.g., `OrderId`, `Money`)
  - Sealed records or classes with private constructor
  - Implement equality explicitly
  - Never nullable (use `Optional<T>` pattern if needed)
- `Interfaces/` — Abstractions (repositories, services) — **implementation is in Infrastructure**
  - `IOrderRepository` — repository contract
  - `IUnitOfWork` — transaction boundaries
  - `IPricingService` — domain service contract
- `Events/` — Domain events (e.g., `OrderCreatedDomainEvent`)
  - Raised by entities
  - Consumed by application layer
- `Exceptions/` — Domain-specific exceptions
  - `OrderDomainException` (order not found, invalid state, etc.)
  - Never generic `InvalidOperationException`
- `Specifications/` — Complex query logic (if using Specification pattern)
  - `HighValueOrderSpecification`
  - `OrdersOverdue Specification`

**Rules**:
- ✅ No `async` — domain logic is synchronous
- ✅ No `DateTime.Now` — inject clock via constructor
- ✅ No I/O — no database, HTTP, or file access
- ✅ No exceptions from infrastructure — return domain exceptions only
- ✅ No null — use `Result<T>` for optional outcomes
- ❌ No interfaces to external services — use application layer
- ❌ No configuration — no `IOptions<T>` or environment variables
- ❌ No logging — logging is infrastructure concern

**Example: Order Aggregate**
```csharp
namespace MyProject.Domain.Orders;

public sealed class Order : IEntity<OrderId>
{
    private readonly List<OrderItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    public OrderId Id { get; }
    public CustomerId CustomerId { get; }
    public List<OrderItem> Items => _items.AsReadOnly();
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order(OrderId id, CustomerId customerId, DateTime createdAt)
    {
        Id = id;
        CustomerId = customerId;
        CreatedAt = createdAt;
        Status = OrderStatus.Pending;
    }

    public static Order Create(CustomerId customerId, List<OrderItem> items)
    {
        Guard.AgainstNull(customerId, nameof(customerId));
        Guard.AgainstEmpty(items, nameof(items));

        var order = new Order(OrderId.New(), customerId, DateTime.UtcNow);
        order._items.AddRange(items);
        order.CalculateTotalAmount();

        order._domainEvents.Add(new OrderCreatedDomainEvent(order.Id, order.CustomerId, order.TotalAmount));

        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException("Can only confirm pending orders");

        Status = OrderStatus.Confirmed;
        _domainEvents.Add(new OrderConfirmedDomainEvent(Id));
    }

    private void CalculateTotalAmount() =>
        TotalAmount = Money.Sum(_items.Select(i => i.LineTotal));
}

public sealed record OrderId(Guid Value) : IEntity<OrderId>
{
    public static OrderId New() => new(Guid.NewGuid());
}

public sealed record OrderItem(ProductId ProductId, int Quantity, Money UnitPrice)
{
    public Money LineTotal => UnitPrice * Quantity;
}

public enum OrderStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }
```

---

### Layer 2: Application (Business Use Cases)

**Responsibility**: Orchestration of domain logic, command/query handling, transaction boundaries

**Contains**:
- `Features/[DomainEntity]/[Operation]/` — Vertical slices
  - `[Operation]Command.cs` — Request model (MediatR IRequest)
  - `[Operation]CommandHandler.cs` — Use case logic
  - `[Operation]Validator.cs` — Input validation (FluentValidation)
  - `[Operation]Request.cs` — Optional: separate DTOs for complex inputs
  - `[Operation]Response.cs` — Response model
- `Common/Behaviors/` — MediatR pipeline behaviors
  - `ValidationBehavior` — auto-validate commands
  - `LoggingBehavior` — log command execution
  - `TransactionBehavior` — wrap in UnitOfWork
- `Common/Interfaces/` — Application service abstractions
  - `IOrderService`
  - `INotificationService`
- `ServiceCollectionExtensions.cs` — DI registration for application layer

**Rules**:
- ✅ Return `Result<T>` from all handlers
- ✅ All async methods accept `CancellationToken`
- ✅ Use `using IUnitOfWork unitOfWork = await _factory.CreateAsync(ct)` pattern
- ✅ Raise domain events and publish to event handlers
- ✅ Inject ILogger for tracing
- ✅ Validate input via FluentValidation
- ✅ Depend on domain layer only (via interfaces)
- ❌ No direct database access (use repositories)
- ❌ No HTTP calls directly (use service abstractions)
- ❌ No async void (except event handlers)

**Example: Create Order Command Handler**
```csharp
namespace MyProject.Application.Features.Orders.Create;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemDto> Items) : IRequest<Result<CreateOrderResponse>>;

public sealed record CreateOrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);

public sealed record CreateOrderResponse(Guid OrderId, decimal TotalAmount, DateTime CreatedAt);

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required");
        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.Quantity).GreaterThan(0);
                item.RuleFor(i => i.UnitPrice).GreaterThan(0);
            });
    }
}

public sealed class CreateOrderCommandHandler(
    IOrderRepository _orderRepository,
    ICustomerService _customerService,
    IUnitOfWork _unitOfWork,
    IPublisher _publisher,
    ILogger<CreateOrderCommandHandler> _logger) : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Validate customer exists
        var customer = await _customerService.GetCustomerAsync(request.CustomerId, ct);
        if (customer is null)
            return Result.Failure<CreateOrderResponse>(
                Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found"));

        // Create domain aggregate
        var items = request.Items
            .Select(i => OrderItem.Create(
                ProductId.Create(i.ProductId),
                i.Quantity,
                Money.Create(i.UnitPrice)))
            .ToList();

        var order = Order.Create(CustomerId.Create(request.CustomerId), items);

        // Persist
        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Publish events
        foreach (var @event in order.DomainEvents)
            await _publisher.Publish(@event, ct);

        _logger.LogInformation("Order {OrderId} created successfully", order.Id.Value);

        return Result.Success(new CreateOrderResponse(
            order.Id.Value,
            order.TotalAmount.Amount,
            order.CreatedAt));
    }
}
```

---

### Layer 3: Infrastructure (External Integrations)

**Responsibility**: Data persistence, external service clients, background jobs, caching

**Contains**:
- `Persistence/` — Database access
  - `ApplicationDbContext.cs` — EF Core DbContext
  - `Repositories/` — Repository implementations (inherit from `IOrderRepository`)
  - `Configurations/` — EF Core entity configurations
  - `Migrations/` — EF Core migrations
- `ExternalServices/` — Third-party integrations
  - `PaymentServiceClient.cs` — HTTP calls to payment provider
  - `EmailServiceClient.cs` — SMTP or SendGrid integration
  - Implement abstractions from application layer
- `BackgroundJobs/` — Hangfire job implementations
  - `SendOrderConfirmationJob.cs` — Publish to queue
- `Caching/` — Cache implementations (if not using SharedCommon.Caching directly)
- `Messaging/` — MassTransit consumer/producer setup
- `ServiceCollectionExtensions.cs` — DI registration for infrastructure

**Rules**:
- ✅ Implement interfaces from application/domain layers
- ✅ Hide EF Core DbContext from above layers (repositories only)
- ✅ Use `async`/`await` for all I/O
- ✅ Inject `ILogger<T>` for tracing
- ✅ Handle external service timeouts gracefully (return `Result<T>`)
- ✅ Implement unit of work pattern for transactions
- ✅ Map EF Core exceptions to domain exceptions
- ✅ Use Polly resilience policies (retry, circuit breaker)
- ❌ No business logic (that's domain responsibility)
- ❌ No direct HTTP responses (that's API layer)
- ❌ Don't leak EF Core types above this layer

**Example: Order Repository**
```csharp
namespace MyProject.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(ApplicationDbContext _context) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct)
    {
        Guard.AgainstNull(order, nameof(order));
        _context.Orders.Add(order);
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken: ct);
    }

    public async Task<List<Order>> ListAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        return await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<int> CountAsync(CancellationToken ct)
    {
        return await _context.Orders.CountAsync(cancellationToken: ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct)
    {
        _context.Orders.Update(order);
    }

    public async Task DeleteAsync(OrderId id, CancellationToken ct)
    {
        var order = await _context.Orders.FindAsync(new object[] { id }, cancellationToken: ct);
        if (order is not null)
            _context.Orders.Remove(order);
    }
}

public sealed class UnitOfWork(ApplicationDbContext _context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Map to domain exception if needed
            throw new OrderDomainException("Failed to persist order changes", ex);
        }
    }

    public void Dispose() => _context.Dispose();
}
```

---

### Layer 4: API (Presentation)

**Responsibility**: HTTP endpoints, request/response mapping, middleware configuration

**Contains**:
- `Features/[DomainEntity]/` — Feature grouping (mirrors Application layer)
  - `[Operation]Endpoint.cs` — Minimal API or controller
  - No business logic — just HTTP adaptation
- `Middleware/` — Custom pipeline middleware
  - Exception handling (via Cerberus.Middlewares)
  - Correlation ID (via Cerberus.Middlewares)
  - Request logging
- `Program.cs` — Application configuration
  - DI registration
  - Middleware pipeline
  - Endpoint mapping
- `appsettings.json` — Default configuration
- `appsettings.{Environment}.json` — Environment-specific overrides

**Rules**:
- ✅ Thin endpoints — delegate to handlers immediately
- ✅ Use `ApiResponse<T>` from ResponseBuilder for all responses
- ✅ Map domain exceptions to HTTP status codes
- ✅ Document endpoints with OpenAPI (Swagger)
- ✅ Use `Result<T>.IsSuccess ? Ok() : mapError()`
- ✅ Accept `CancellationToken` from HTTP context
- ✅ Inject `IResponseBuilder` for standardized responses
- ✅ All configuration via `IOptions<T>` and `appsettings.json`
- ❌ No business logic in endpoints
- ❌ No direct database access
- ❌ No hardcoded values (ports, connection strings, etc.)
- ❌ No `async void` except event handlers

**Example: Create Order Endpoint**
```csharp
namespace MyProject.Api.Features.Orders;

public static class CreateOrderEndpoint
{
    public static void MapCreateOrder(this WebApplication app)
    {
        app.MapPost("/orders", Handle)
            .WithName("CreateOrder")
            .WithOpenApi()
            .Produces<ApiResponse<CreateOrderResponse>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        CreateOrderCommand command,
        ISender sender,
        IResponseBuilder responseBuilder,
        ILogger<CreateOrderEndpoint> logger,
        CancellationToken ct)
    {
        logger.LogInformation("POST /orders - Creating order for customer {CustomerId}", command.CustomerId);

        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? responseBuilder.Success(result.Value)
                .WithStatusCode(StatusCodes.Status201Created)
                .Build()
            : responseBuilder.Failure(result.Error)
                .Build();
    }
}

public static class OrderEndpointExtensions
{
    public static void MapOrderFeatures(this WebApplication app)
    {
        app.MapCreateOrder();
        app.MapGetOrder();
        app.MapListOrders();
        app.MapUpdateOrder();
        app.MapDeleteOrder();
    }
}
```

---

## Vertical Slice Architecture

**Principle**: Features are self-contained, independent, and can be developed/deployed separately.

### Feature Structure

```
Features/
├── Orders/
│   ├── Create/
│   │   ├── CreateOrderCommand.cs        (request)
│   │   ├── CreateOrderCommandHandler.cs (logic)
│   │   ├── CreateOrderValidator.cs      (validation)
│   │   ├── CreateOrderEndpoint.cs       (HTTP)
│   │   ├── CreateOrderResponse.cs       (response)
│   │   └── CreateOrderTests.cs          (tests in test project)
│   ├── Get/
│   │   ├── GetOrderQuery.cs
│   │   ├── GetOrderQueryHandler.cs
│   │   └── GetOrderEndpoint.cs
│   ├── List/
│   │   └── ...
│   ├── Update/
│   │   └── ...
│   └── Delete/
│       └── ...
├── Products/
│   ├── Create/
│   ├── Get/
│   └── ...
└── Customers/
    └── ...
```

### Benefits

1. **Cohesion**: All code for a feature is in one place
2. **Independence**: Features can be developed in parallel
3. **Testability**: Feature tests in same folder (or mirror in test project)
4. **Discoverability**: Easy to find code related to a feature
5. **Scalability**: Can be extracted to microservice later
6. **Navigation**: Clear folder structure reduces cognitive load

---

## Dependency Injection Pattern

### Registration Order (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Cerberus shared services (always first)
builder.Services
    .AddSharedLogging()
    .AddSharedObservability()
    .AddSharedHealthChecks()
    .AddSharedValidation()
    .AddSharedResponseBuilder()
    .AddSharedMiddlewares()
    .AddSharedCaching(builder.Configuration)           // Optional
    .AddSharedMessaging(builder.Configuration)         // Optional
    .AddSharedCloud(builder.Configuration);            // Optional

// 2. Application services (use cases, handlers)
builder.Services.AddApplicationServices();

// 3. Infrastructure services (repositories, DbContext)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 4. API-specific services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => ...);

var app = builder.Build();

// Middleware pipeline (order matters)
app.UseSharedExceptionHandler()
    .UseSharedCorrelationId()
    .UseSharedRequestLogging()
    .UseSwagger()
    .UseSwaggerUI()
    .UseCors();

app.MapSharedHealthChecks();
app.MapOrderFeatures();
app.MapProductFeatures();

app.Run();
```

### Extension Methods (ServiceCollectionExtensions.cs)

**Application Layer**:
```csharp
namespace MyProject.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
```

**Infrastructure Layer**:
```csharp
namespace MyProject.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>();

        return services;
    }
}
```

---

## Configuration Management

### Appsettings Hierarchy

1. **appsettings.json** — Default for all environments
2. **appsettings.{Environment}.json** — Environment-specific (Development, Staging, Production)
3. **User Secrets** (development only) — Sensitive values
4. **Environment Variables** (production) — Cloud-injected secrets

### Example appsettings.json

```json
{
  "ApplicationName": "MyProject",
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
  "Database": {
    "Provider": "SqlServer",
    "MigrationAssembly": "MyProject.Infrastructure"
  },
  "Caching": {
    "Enabled": false,
    "L1": { "MaxSizeMb": 100, "TtlSeconds": 300 },
    "L2": { "ConnectionString": "", "TtlSeconds": 3600 }
  },
  "Messaging": {
    "Enabled": false,
    "Transport": "RabbitMQ",
    "RabbitMQ": { "Host": "localhost", "Port": 5672 }
  },
  "Cloud": {
    "Enabled": false,
    "Provider": "Azure"
  }
}
```

### Usage Pattern

```csharp
public sealed class OrderService(
    IOptions<CachingOptions> cachingOptions,
    IOptions<MessagingOptions> messagingOptions)
{
    private readonly CachingOptions _caching = cachingOptions.Value;
    private readonly MessagingOptions _messaging = messagingOptions.Value;

    // Use _caching and _messaging
}
```

---

## Testing Strategy

### Unit Tests (Domain & Application)

```csharp
namespace MyProject.Tests.Unit.Application.Orders;

public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(customerId, new List<CreateOrderItemDto>
        {
            new(Guid.NewGuid(), 2, 99.99m)
        });

        var mockRepository = new Mock<IOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateOrderCommandHandler(
            mockRepository.Object,
            Mock.Of<ICustomerService>(),
            mockUnitOfWork.Object,
            Mock.Of<IPublisher>(),
            Mock.Of<ILogger<CreateOrderCommandHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().NotBe(Guid.Empty);
        mockRepository.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCustomerId_ReturnNotFound()
    {
        // Arrange
        var command = new CreateOrderCommand(Guid.Empty, new List<CreateOrderItemDto>());
        var mockCustomerService = new Mock<ICustomerService>();
        mockCustomerService.Setup(x => x.GetCustomerAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        // Act & Assert
        var handler = new CreateOrderCommandHandler(...);
        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }
}
```

### Integration Tests

```csharp
namespace MyProject.Tests.Integration.Features.Orders;

public class CreateOrderIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateOrderIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateOrder_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateOrderCommand(Guid.NewGuid(), new List<CreateOrderItemDto>
        {
            new(Guid.NewGuid(), 2, 99.99m)
        });

        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var content = await response.Content.ReadAsAsync<ApiResponse<CreateOrderResponse>>();
        content.Success.Should().BeTrue();
    }
}

public sealed class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Use in-memory database for tests
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    }
}
```

---

## Error Handling & Result Pattern

### Domain Exceptions Hierarchy

```csharp
public abstract class DomainException : Exception
{
    public string Code { get; protected set; }
    public int StatusCode { get; protected set; }

    protected DomainException(string message, string code, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public sealed class OrderDomainException : DomainException
{
    public OrderDomainException(string message, string code = "Order.Error", int statusCode = 400)
        : base(message, code, statusCode) { }
}

public sealed class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(Guid orderId)
        : base($"Order {orderId} not found", "Order.NotFound", 404) { }
}
```

### Result<T> Pattern

```csharp
// Always return Result from application handlers
public async Task<Result<CreateOrderResponse>> Handle(CreateOrderCommand request, CancellationToken ct)
{
    // Validation failure
    if (validation fails)
        return Result.Failure<CreateOrderResponse>(
            Error.Validation("Order.InvalidItems", "Orders must have at least one item"));

    // Not found
    var customer = await _customerService.GetAsync(request.CustomerId, ct);
    if (customer is null)
        return Result.Failure<CreateOrderResponse>(
            Error.NotFound("Customer.NotFound", $"Customer {request.CustomerId} not found"));

    // Domain failure
    try
    {
        var order = Order.Create(...);
        // ...
        return Result.Success(response);
    }
    catch (OrderDomainException ex)
    {
        _logger.LogWarning(ex, "Order creation failed: {Code}", ex.Code);
        return Result.Failure<CreateOrderResponse>(
            Error.FromException(ex));
    }
}
```

### HTTP Mapping (in Endpoint)

```csharp
private static IResult Handle(ISender sender, ...)
{
    var result = await sender.Send(command, ct);

    return result.IsSuccess
        ? responseBuilder.Success(result.Value).Build()
        : result.Error.Code switch
        {
            "Order.NotFound" => responseBuilder.Failure(result.Error)
                .WithStatusCode(StatusCodes.Status404NotFound)
                .Build(),
            "Order.InvalidItems" => responseBuilder.Failure(result.Error)
                .WithStatusCode(StatusCodes.Status400BadRequest)
                .Build(),
            "Customer.NotFound" => responseBuilder.Failure(result.Error)
                .WithStatusCode(StatusCodes.Status404NotFound)
                .Build(),
            _ => responseBuilder.Failure(result.Error)
                .WithStatusCode(StatusCodes.Status500InternalServerError)
                .Build()
        };
}
```

---

## Cerberus Package Integration

### Essential Packages (Always Include)

| Package | Purpose | Integration Point |
|---------|---------|------------------|
| Core | `Result<T>`, `Guard`, domain exceptions | All layers |
| Logging | Structured Serilog logging | All layers |
| Middlewares | CorrelationId, exception handling, request logging | API layer, Program.cs |
| ResponseBuilder | Standardized API responses | Endpoints |
| HealthChecks | Service health status | Program.cs |
| Validation | FluentValidation DI | Application commands |
| Observability | OpenTelemetry tracing/metrics | All layers |

### Optional Packages (Config-Driven)

| Package | When | Configuration |
|---------|------|---------------|
| Caching | Performance-critical queries | `Caching.Enabled: true` |
| Messaging | Event publishing, async workflows | `Messaging.Enabled: true` + `Transport: RabbitMQ\|Kafka` |
| Cloud | Cloud blob/secrets/queues | `Cloud.Enabled: true` + `Provider: Azure\|Aws` |
| MultiTenancy | SaaS, multi-tenant data isolation | `MultiTenancy.Enabled: true` |
| Auditing | Compliance, audit trail | `Auditing.Enabled: true` |
| BackgroundJobs | Long-running tasks | `BackgroundJobs.Enabled: true` |

---

## Best Practices Checklist

- [ ] All layers properly separated (no circular references)
- [ ] Domain logic is in Domain layer, not Application or API
- [ ] All I/O is async with CancellationToken
- [ ] Result<T> used for expected failures, exceptions for unexpected
- [ ] Dependency injection configured via Program.cs extensions
- [ ] No hardcoded values (connection strings, ports, API keys)
- [ ] Structured logging with correlation IDs throughout
- [ ] All public APIs have XML documentation
- [ ] Unit tests mock external dependencies
- [ ] Integration tests use in-memory database or test container
- [ ] Environment configuration via appsettings.{Environment}.json
- [ ] Sensitive values in user-secrets (dev) or env vars (prod)
- [ ] OpenAPI docs auto-generated from endpoints
- [ ] Health checks configured (liveness + readiness)
- [ ] CORS policy configured per environment
- [ ] Rate limiting configured for public endpoints
- [ ] Caching strategy (L1 + L2) for performance
- [ ] Error responses use ProblemDetails (RFC 9457)
- [ ] Pagination on all list endpoints
- [ ] Soft deletes considered for audit trail

---

## Common Patterns

### Implement a New Feature

1. Create `Features/[Entity]/[Operation]/` folder
2. Add `[Operation]Command.cs` (request + validator)
3. Add `[Operation]CommandHandler.cs` (logic)
4. Add `[Operation]Endpoint.cs` (HTTP mapping)
5. Register in feature root extension method
6. Add integration tests
7. Document in OpenAPI

### Add Caching to Query Handler

```csharp
var cacheKey = $"order:{id}";
var cached = await _cache.GetAsync<OrderDto>(cacheKey, ct);
if (cached is not null)
    return Result.Success(cached);

var order = await _repository.GetByIdAsync(id, ct);
await _cache.SetAsync(cacheKey, order, TimeSpan.FromHours(1), ct);
return Result.Success(order);
```

### Publish Domain Event

```csharp
var order = Order.Create(...);
await _repository.AddAsync(order, ct);
await _unitOfWork.SaveChangesAsync(ct);

foreach (var @event in order.DomainEvents)
    await _publisher.Publish(@event, ct);
```

### Handle External Service Timeout

```csharp
try
{
    var result = await _paymentService.ProcessAsync(payment, ct);
    return Result.Success(result);
}
catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
{
    _logger.LogWarning(ex, "Payment service timeout for payment {PaymentId}", payment.Id);
    return Result.Failure<PaymentResult>(
        Error.ServiceUnavailable("Payment.Timeout", "Payment processing temporarily unavailable"));
}
```

---

## Security Considerations

- ✅ Secrets in `appsettings.{Environment}.json` (gitignored)
- ✅ User Secrets for development
- ✅ Environment variables for production
- ✅ Input validation on all endpoints
- ✅ Authorization policies (not role-based)
- ✅ No PII in logs
- ✅ SQL injection prevention (via ORM)
- ✅ CORS configured
- ✅ Rate limiting on public endpoints
- ✅ Content-Type validation
- ✅ Request size limits
- ✅ Response compression enabled

---

## Performance Optimization

- ✅ Async/await throughout
- ✅ Database query optimization (projections, indices)
- ✅ Connection pooling (EF Core auto)
- ✅ Response caching (HTTP + L1/L2)
- ✅ Pagination on list endpoints
- ✅ Lazy loading (or eager as needed)
- ✅ Bulk operations where applicable
- ✅ Database query timeouts configured
- ✅ Distributed tracing to identify bottlenecks

---

## Deployment Considerations

- ✅ Environment-based configuration
- ✅ Database migrations automated
- ✅ Health checks for orchestrator liveness/readiness
- ✅ Structured logs for centralized logging
- ✅ OpenTelemetry exporters configured
- ✅ HTTPS enforced in production
- ✅ CORS restricted to known origins
- ✅ Rate limiting enabled
- ✅ Container-ready (Dockerfile included)
- ✅ 12-factor app compliance

---

**Last Updated**: 2026-05-29  
**Compatible With**: Cerberus v3.0+, .NET 8.0+
