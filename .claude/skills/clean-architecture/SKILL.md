# Clean Architecture Skill

Enforce correct layering, service boundaries, and DI design.

## When to Use This Skill

Triggers:
- Designing API layer structure
- Building request/response models
- Setting up dependency injection composition root
- Defining service boundaries across packages

Ask Claude explicitly: "Use clean-architecture skill"

## Input (What You Provide)

- API or service design description
- Refactoring goal

## Output (What You Get)

- Validated layer structure
- Dependency direction confirmation
- DI registration guidance

## Layer Definitions

```
Presentation  →  Application  →  Domain  ←  Infrastructure
(Controllers)    (UseCases)    (Entities)   (EF, HTTP, Redis)
```

- Arrows show allowed dependency direction
- Domain has no outward dependencies
- Infrastructure implements Domain abstractions

## Checklist

**Presentation Layer:**
- [ ] No business logic
- [ ] Thin controllers / minimal APIs
- [ ] Input validation (FluentValidation or DataAnnotations)
- [ ] Maps to/from Application DTOs
- [ ] Returns standardized responses (ResponseBuilder)

**Application Layer:**
- [ ] Orchestrates use cases
- [ ] No direct infrastructure access
- [ ] Uses domain abstractions (IRepository, IService)
- [ ] Returns Result<T>, not throws
- [ ] CancellationToken on all async methods

**Domain Layer:**
- [ ] Pure business logic and rules
- [ ] No framework dependencies
- [ ] Rich domain models or anemic + services (consistent choice)
- [ ] Domain events for cross-aggregate communication

**Infrastructure Layer:**
- [ ] Implements domain abstractions
- [ ] All I/O lives here
- [ ] No business logic
- [ ] Registered in DI as implementations of domain interfaces

## DI Registration Pattern

```csharp
// Package extension method — always use this pattern
public static IServiceCollection AddOrderService(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<OrderOptions>(configuration.GetSection("Orders"));
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IOrderRepository, EfOrderRepository>();
    return services;
}
```

## Common Mistakes

❌ Controller directly instantiates services (`new OrderService()`)
- Fix: Inject via constructor

❌ Service calls `HttpContext` directly
- Fix: Extract to ICurrentUserAccessor, inject it

❌ Domain entity has EF navigation properties as public setters
- Fix: Use private setters + factory methods

## References

See: docs/architecture/layering.md
See: docs/architecture/dependency-rules.md
See: docs/standards/api-design.md
