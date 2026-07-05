# Cloud-Ready DDD Template — Quick Reference

Fast lookup for common tasks and patterns.

## Project Structure at a Glance

```
YourService/
├── src/
│   ├── YourService.Domain/               # Entities, Aggregates, Value Objects (NO external deps)
│   │   ├── Common/                       # IEntity, IDomainEvent, AggregateRoot
│   │   ├── Features/FeatureName/         # One aggregate per domain folder
│   │   └── Features/FeatureName/Events/  # Domain events
│   │
│   ├── YourService.Application/          # Use Cases, Commands, Queries (Domain-dependent only)
│   │   ├── Common/                       # Pipeline behaviors, DTOs, exceptions
│   │   ├── Features/FeatureName/Command/ # Commands with handlers & validators
│   │   └── Features/FeatureName/Query/   # Queries with handlers
│   │
│   ├── YourService.Infrastructure/       # EF Core, Repositories, External Services
│   │   ├── Persistence/                  # DbContext, Migrations, Repositories
│   │   ├── Services/                     # Cache, Message Broker, Storage clients
│   │   └── ServiceCollectionExtensions   # DI registration
│   │
│   └── YourService.Api/                  # ASP.NET Core, Endpoints, Middleware
│       ├── Endpoints/                    # Minimal API endpoint groups
│       ├── Features/                     # Feature-specific endpoint logic
│       ├── Middleware/                   # Custom middleware
│       ├── Program.cs                    # Application startup & DI
│       └── appsettings.json              # Configuration
│
├── tests/
│   ├── YourService.UnitTests/            # Unit tests (no DB, no HTTP)
│   ├── YourService.IntegrationTests/     # Integration tests (with InMemory DB)
│   └── YourService.ArchitectureTests/    # Layer separation, dependency rules
│
└── docs/
    ├── README.md                         # Overview
    ├── GETTING_STARTED.md                # Setup & first feature
    ├── DEPLOYMENT_GUIDE.md               # Production deployment
    ├── BEST_PRACTICES_CHECKLIST.md       # Quality standards
    ├── DATABASE_SCHEMA.md                # ER diagram & tables
    └── API_DOCUMENTATION.md              # Endpoint reference
```

## Creating a New Feature

### 1. Create Domain Aggregate

**File**: `src/YourService.Domain/Features/Orders/Order.cs`
```csharp
public class Order : AggregateRoot
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items { get; private set; }

    private Order() { } // EF Core only

    public static Result<Order> Create(CustomerId customerId, List<OrderItem> items)
    {
        Guard.NotNull(customerId, nameof(customerId));
        Guard.NotEmpty(items, nameof(items));

        var order = new Order
        {
            Id = OrderId.CreateNew(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Items = items
        };

        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id, customerId));
        return Result.Success(order);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("OrderAlreadyConfirmed", "Cannot confirm non-pending order", StatusCodes.Status400BadRequest);

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderConfirmedDomainEvent(Id));
    }
}

public record OrderId(Guid Value)
{
    public static OrderId CreateNew() => new(Guid.NewGuid());
}

public enum OrderStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }
```

### 2. Create Repository Interface

**File**: `src/YourService.Domain/Features/Orders/IOrderRepository.cs`
```csharp
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> ListByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);
}
```

### 3. Create Domain Events

**File**: `src/YourService.Domain/Features/Orders/Events/OrderDomainEvents.cs`
```csharp
public record OrderCreatedDomainEvent(OrderId OrderId, CustomerId CustomerId) : DomainEvent;
public record OrderConfirmedDomainEvent(OrderId OrderId) : DomainEvent;
```

### 4. Create Command

**File**: `src/YourService.Application/Features/Orders/Create/CreateOrderCommand.cs`
```csharp
public record CreateOrderCommand(
    Guid CustomerId,
    List<CreateOrderItemDto> Items
) : IRequest<Result<CreateOrderResponse>>;

public record CreateOrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateOrderResponse(Guid OrderId, decimal TotalAmount, DateTime CreatedAt);
```

### 5. Create Validator

**File**: `src/YourService.Application/Features/Orders/Create/CreateOrderCommandValidator.cs`
```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required");

        RuleForEach(x => x.Items)
            .ChildRules(child =>
            {
                child.RuleFor(i => i.ProductId).NotEmpty();
                child.RuleFor(i => i.Quantity).GreaterThan(0);
            });
    }
}
```

### 6. Create Handler

**File**: `src/YourService.Application/Features/Orders/Create/CreateOrderCommandHandler.cs`
```csharp
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var customerId = new CustomerId(request.CustomerId);
            var items = request.Items
                .Select(i => new OrderItem(
                    new ProductId(i.ProductId),
                    i.Quantity,
                    new Money(i.UnitPrice)))
                .ToList();

            var result = Order.Create(customerId, items);
            if (!result.IsSuccess)
                return Result.Failure<CreateOrderResponse>(result.Error!);

            var order = result.Value!;
            await _repository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain events
            foreach (var @event in order.DomainEvents)
                await _publisher.Publish(@event, cancellationToken);

            order.ClearDomainEvents();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CreateOrderResponse(
                order.Id.Value,
                order.Items.Sum(i => i.LineTotal.Amount),
                DateTime.UtcNow);

            return Result.Success(response);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain validation failed: {Code} - {Message}", ex.Code, ex.Message);
            return Result.Failure<CreateOrderResponse>(new Error(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return Result.Failure<CreateOrderResponse>(new Error("InternalServerError", "An unexpected error occurred"));
        }
    }
}
```

### 7. Create API Endpoint

**File**: `src/YourService.Api/Features/Orders/CreateOrderEndpoint.cs`
```csharp
public static class OrderEndpoints
{
    public static void MapOrderFeatures(this WebApplication app)
    {
        var group = app.MapGroup("/orders")
            .WithName("Orders")
            .WithOpenApi();

        group.MapPost("/", CreateOrder)
            .WithName("Create Order")
            .WithSummary("Create a new order")
            .Produces<ApiResponse<CreateOrderResponse>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .WithOpenApi();
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/orders/{result.Value!.OrderId}", result.Value)
            : Results.BadRequest(result.Error);
    }
}

// In Program.cs
app.MapOrderFeatures();
```

### 8. Write Unit Test

**File**: `tests/YourService.UnitTests/Features/Orders/CreateOrderCommandHandlerTests.cs`
```csharp
public class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CreatesOrderSuccessfully()
    {
        // Arrange
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), 5, 99.99m)
            });

        var mockRepository = new Mock<IOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockPublisher = new Mock<IPublisher>();
        var logger = new Mock<ILogger<CreateOrderCommandHandler>>();

        var handler = new CreateOrderCommandHandler(
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockPublisher.Object,
            logger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().NotBeEmpty();
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## Common Patterns

### Using Result<T> for Expected Failures

```csharp
// Instead of throwing exceptions for expected failures:
public Result<Order> ValidateOrder(Order order)
{
    if (order.Items.Count == 0)
        return Result.Failure<Order>(new Error("EmptyOrder", "Orders must have items"));

    if (order.TotalAmount < 0)
        return Result.Failure<Order>(new Error("InvalidTotal", "Total amount cannot be negative"));

    return Result.Success(order);
}

// Usage:
var result = ValidateOrder(order);
if (!result.IsSuccess)
    return result.Error; // Return error to client
```

### Guard Clauses for Invariants

```csharp
public void Transfer(Account fromAccount, Account toAccount, Money amount)
{
    Guard.NotNull(fromAccount, nameof(fromAccount));
    Guard.NotNull(toAccount, nameof(toAccount));
    Guard.NotNull(amount, nameof(amount));
    Guard.AgainstExpression(a => a.Amount <= 0, amount, nameof(amount));

    if (fromAccount.Balance.Amount < amount.Amount)
        throw new DomainException("InsufficientFunds", "Account does not have sufficient balance", StatusCodes.Status400BadRequest);

    // Safe to proceed
}
```

### MediatR Pipeline with FluentValidation

```csharp
// Automatic validation via ValidationBehavior
var result = await mediator.Send(new CreateOrderCommand(...));
// If invalid, ValidationBehavior intercepts and returns failure
// If valid, handler executes

// In ServiceCollectionExtensions:
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

### Repository with EF Core

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(order, nameof(order));
        await _context.Set<Order>().AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(id, nameof(id));
        return await _context.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Order>> ListByCustomerAsync(
        CustomerId customerId,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(customerId, nameof(customerId));
        return await _context.Set<Order>()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
```

---

## Configuration Quick Reference

### Enable Caching

**appsettings.json**:
```json
{
  "Features": {
    "Caching": {
      "Enabled": true,
      "L1": {
        "MaxSizeMb": 500,
        "TtlSeconds": 600
      },
      "L2": {
        "ConnectionString": "redis:6379",
        "TtlSeconds": 3600,
        "Enabled": true
      }
    }
  }
}
```

**Program.cs**:
```csharp
if (cacheOptions.Enabled)
{
    services.AddSharedCaching(cacheOptions);
}
```

### Enable Messaging (Kafka)

**appsettings.json**:
```json
{
  "Features": {
    "Messaging": {
      "Enabled": true,
      "Provider": "Kafka",
      "Kafka": {
        "Servers": "kafka:9092",
        "GroupId": "yourservice-group"
      }
    }
  }
}
```

### Enable Auditing

**appsettings.json**:
```json
{
  "Features": {
    "Auditing": {
      "Enabled": true,
      "Backend": "Database"
    }
  }
}
```

---

## Debugging Checklist

**Application Won't Start:**
```bash
# Check for configuration errors
dotnet run --verbosity debug

# Check environment variables
$env:ASPNETCORE_ENVIRONMENT
$env:ConnectionStrings__DefaultConnection

# Validate appsettings.json
dotnet run -- --help
```

**Test Failures:**
```bash
# Run specific test
dotnet test --filter "TestClass.TestMethod"

# Run with verbose output
dotnet test -v n

# Run with logging
dotnet test -- --logger "console;verbosity=detailed"
```

**Database Issues:**
```bash
# List migrations
dotnet ef migrations list

# Update database
dotnet ef database update

# Reset database (dev only)
dotnet ef database drop --force
dotnet ef database update
```

**Performance Issues:**
```bash
# Enable query logging
# In appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Debug"
    }
  }
}
```

---

## CLI Commands

### Create New Project

```powershell
# Using template script
.\scripts\create-project.ps1 -ProjectName MyNewService -OutputPath C:\Projects
```

### Build & Test

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/YourService.UnitTests/YourService.UnitTests.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Database Operations

```bash
# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Script migration for review
dotnet ef migrations script -o migration.sql
```

### Package Management

```bash
# Check for outdated packages
dotnet package search Microsoft.EntityFrameworkCore

# Update specific package
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0

# Update all packages (in project file directory)
dotnet restore
```

---

## Important Reminders

1. **Never hardcode secrets** - use User Secrets (dev) or KeyVault (production)
2. **Always use async/await** - no `.Result`, `.Wait()`, or blocking calls
3. **Pass CancellationToken through** - enable graceful shutdown
4. **Validate at boundaries** - guard clauses at method entry
5. **No infrastructure in Domain** - keeps domain testable and pure
6. **Publish domain events** - communicate state changes to application layer
7. **Use Result<T> for expected failures** - not exceptions for business logic
8. **Test behavior, not implementation** - focus on what, not how
9. **Keep endpoints thin** - orchestration belongs in application layer
10. **Log context, not implementation** - help debugging without revealing internals

---

**Last Updated**: 2026-05-30
