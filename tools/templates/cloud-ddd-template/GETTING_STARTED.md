# Cloud-Ready DDD Template — Getting Started Guide

This guide walks through using the cloud-ready DDD project template to build a new service.

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server, PostgreSQL, or SQLite
- (Optional) Redis for caching
- (Optional) RabbitMQ or Kafka for messaging

## Installation

### Clone the Template

```bash
# Clone Cerberus repository
git clone <cerberus-repo-url>
cd Cerberus/tools/templates/cloud-ddd-template

# Or download template via dotnet (when published)
dotnet new install ./
```

### Create Your Project

**Option 1: Using PowerShell script (recommended)**
```powershell
.\scripts\create-project.ps1 -ProjectName "YourService"
```

**Option 2: Manual copy**
```bash
cp -r cloud-ddd-template YourService
cd YourService
# Update .sln, .csproj files with your project name
```

## Post-Installation Setup

### 1. Update Project Names

Replace all instances of `Templates` with your project name:

```powershell
# On Windows
Get-ChildItem -Recurse -Include "*.csproj" | ForEach-Object {
    (Get-Content $_) -replace "Templates", "YourService" | Set-Content $_
}
```

### 2. Update Solution File

```bash
# Rename projects in the .sln file
YourService.sln
src/YourService.Api/
src/YourService.Application/
src/YourService.Domain/
src/YourService.Infrastructure/
```

### 3. Configure Database

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourServiceDb;User=sa;Password=YourPassword;"
  }
}
```

### 4. Restore and Build

```bash
dotnet restore
dotnet build
```

### 5. Create Database

```bash
cd src/YourService.Infrastructure
dotnet ef database update
```

### 6. Run the Application

```bash
cd src/YourService.Api
dotnet run
```

Visit `http://localhost:5000/swagger` to explore the API.

## Project Structure Overview

```
YourService/
├── src/
│   ├── YourService.Domain/           # Business logic (entities, value objects)
│   ├── YourService.Application/      # Use cases (commands, handlers)
│   ├── YourService.Infrastructure/   # Data access (repositories, DbContext)
│   └── YourService.Api/              # HTTP endpoints, middleware
├── tests/
│   ├── YourService.UnitTests/        # Domain & application tests
│   └── YourService.IntegrationTests/ # API & end-to-end tests
└── YourService.sln
```

## Adding Your First Feature

### 1. Define the Domain Model

**src/YourService.Domain/Features/[Entity]/[Entity].cs**:
```csharp
namespace YourService.Domain.Features.Products;

public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }

    public static Product Create(string name, string description, decimal price)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));
        Guard.AgainstLessThan(price, 0, nameof(price));

        var product = new Product
        {
            Id = ProductId.New(),
            Name = name,
            Description = description,
            Price = price
        };

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id.Value, name));
        return product;
    }
}
```

### 2. Create Repository Interface

**src/YourService.Domain/Features/[Entity]/IProductRepository.cs**:
```csharp
public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(ProductId id, CancellationToken ct = default);
}
```

### 3. Implement Repository

**src/YourService.Infrastructure/Persistence/Repositories/ProductRepository.cs**:
```csharp
public sealed class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        Guard.AgainstNull(product, nameof(product));
        context.Set<Product>().Add(product);
        await Task.CompletedTask;
    }

    // ... implement other methods
}
```

### 4. Create Application Handler

**src/YourService.Application/Features/Products/Create/CreateProductCommand.cs**:
```csharp
public sealed record CreateProductCommand(string Name, string Description, decimal Price)
    : IRequest<Result<CreateProductResponse>>;

public sealed record CreateProductResponse(Guid ProductId, string Name);
```

**src/YourService.Application/Features/Products/Create/CreateProductCommandHandler.cs**:
```csharp
public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateProductCommandHandler> logger) : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Creating product: {ProductName}", request.Name);

        var product = Product.Create(request.Name, request.Description, request.Price);

        await productRepository.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Product {ProductId} created", product.Id.Value);

        return Result.Success(new CreateProductResponse(product.Id.Value, product.Name));
    }
}
```

### 5. Create Endpoint

**src/YourService.Api/Features/Products/ProductEndpoints.cs**:
```csharp
public static void MapProductFeatures(this WebApplication app)
{
    var group = app.MapGroup("/products")
        .WithName("Products")
        .WithTags("Products");

    group.MapPost("", CreateProduct)
        .WithName("CreateProduct")
        .WithSummary("Create a new product")
        .Produces<ApiResponse<CreateProductResponse>>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
}

private static async Task<IResult> CreateProduct(
    [FromBody] CreateProductCommand command,
    ISender sender,
    IResponseBuilder responseBuilder,
    CancellationToken ct)
{
    var result = await sender.Send(command, ct);
    return result.IsSuccess
        ? responseBuilder.Success(result.Value).WithStatusCode(StatusCodes.Status201Created).Build()
        : responseBuilder.Failure(result.Error).Build();
}
```

### 6. Register in Program.cs

```csharp
// In Program.cs
services.AddScoped<IProductRepository, ProductRepository>();

// Map endpoints
app.MapProductFeatures();
```

### 7. Write Tests

**tests/YourService.UnitTests/Domain/Products/ProductTests.cs**:
```csharp
public class ProductTests
{
    [Fact]
    public void Create_WithValidInput_CreatesProduct()
    {
        var product = Product.Create("Widget", "A useful widget", 29.99m);

        product.Should().NotBeNull();
        product.Name.Should().Be("Widget");
    }
}
```

## Enabling Optional Features

### Enable Caching

**appsettings.json**:
```json
{
  "Features": {
    "Caching": {
      "Enabled": true,
      "L1": { "MaxSizeMb": 100, "TtlSeconds": 300 },
      "L2": { "ConnectionString": "localhost:6379", "TtlSeconds": 3600 }
    }
  }
}
```

**Program.cs**:
```csharp
if (builder.Configuration.GetValue<bool>("Features:Caching:Enabled"))
    builder.Services.AddSharedCaching(builder.Configuration);
```

**In handlers**:
```csharp
var cached = await _cacheService.GetAsync<Product>($"product:{id}", ct);
if (cached is not null)
    return Result.Success(new CreateProductResponse(cached.Id.Value, cached.Name));

// ... fetch from database ...

await _cacheService.SetAsync($"product:{id}", product, TimeSpan.FromHours(1), ct);
```

### Enable Messaging

**appsettings.json**:
```json
{
  "Features": {
    "Messaging": {
      "Enabled": true,
      "Transport": "RabbitMQ|Kafka",
      "RabbitMQ": { "Host": "localhost", "Port": 5672 },
      "Kafka": { "BootstrapServers": "localhost:9092" }
    }
  }
}
```

### Enable Multi-Tenancy

**appsettings.json**:
```json
{
  "Features": {
    "MultiTenancy": {
      "Enabled": true,
      "Strategy": "Header|JwtClaim|Subdomain|QueryString"
    }
  }
}
```

**In handlers**:
```csharp
public sealed class CreateProductCommandHandler(
    ITenantContext tenantContext,
    // ... other dependencies
) : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId; // Use tenant context
        // ... rest of handler
    }
}
```

## Database Migrations

### Create a Migration

```bash
cd src/YourService.Infrastructure
dotnet ef migrations add AddProductTable
```

### Apply Migrations

```bash
dotnet ef database update
```

### Revert Migration

```bash
dotnet ef migrations remove  # Remove last migration
dotnet ef database update    # Reapply previous state
```

## Running Tests

### Unit Tests

```bash
dotnet test tests/YourService.UnitTests
```

### Integration Tests

```bash
dotnet test tests/YourService.IntegrationTests
```

### All Tests

```bash
dotnet test
```

### With Coverage

```bash
dotnet test /p:CollectCoverage=true
```

## Troubleshooting

### Connection String Error

**Error**: "Connection string 'DefaultConnection' not found"

**Solution**: Ensure `appsettings.json` has a valid connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourServiceDb;..."
  }
}
```

### Database Migration Failed

**Error**: "Unable to apply migration"

**Solution**:
1. Check database connectivity
2. Verify SQL Server is running
3. Try manual migration:
   ```bash
   dotnet ef database drop
   dotnet ef database update
   ```

### Tests Won't Build

**Error**: Missing xUnit or test dependencies

**Solution**:
```bash
dotnet restore tests/
dotnet test --no-restore
```

### API Won't Start

**Error**: Port already in use

**Solution**: Change port in `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5001" }
    }
  }
}
```

## Deployment

### Docker

Create `Dockerfile` in project root:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/YourService.Api/", "src/YourService.Api/"]
RUN dotnet publish "src/YourService.Api/YourService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "YourService.Api.dll"]
```

Build and run:
```bash
docker build -t yourservice:latest .
docker run -p 5000:5000 yourservice:latest
```

### Kubernetes

Create `deployment.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: yourservice
spec:
  replicas: 3
  selector:
    matchLabels:
      app: yourservice
  template:
    metadata:
      labels:
        app: yourservice
    spec:
      containers:
      - name: yourservice
        image: yourservice:latest
        ports:
        - containerPort: 5000
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
```

Deploy:
```bash
kubectl apply -f deployment.yaml
```

## Best Practices

✅ **DO:**
- Keep handlers focused on single responsibility
- Use MediatR for command/query separation
- Validate input in validators
- Log important business events
- Write tests for business logic
- Use repositories for data access
- Keep domain logic in aggregates

❌ **DON'T:**
- Mix concerns across layers
- Put business logic in endpoints
- Hardcode configuration values
- Skip validation
- Ignore error handling
- Use static methods for services
- Access database from domain layer

## References

- [Template CLAUDE.md](./CLAUDE.md) — Architecture guidelines
- [Cerberus Documentation](../../../docs/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://domainlanguage.com/ddd/)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)

## Support

For issues or questions:
1. Check [Template CLAUDE.md](./CLAUDE.md)
2. Review Cerberus [docs/](../../../docs/)
3. Refer to example features (Orders)

---

**Last Updated**: 2026-05-29
