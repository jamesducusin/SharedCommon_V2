# Cloud-Ready DDD Template — Summary

This directory contains a **production-ready project template** for building enterprise-grade microservices with:
- **Clean Architecture** (Domain → Application → Infrastructure → Presentation)
- **Domain-Driven Design** (Aggregates, Value Objects, Domain Services)
- **Vertical Slice Architecture** (Features organized as independent, cohesive slices)
- **Cloud-Ready** (12-factor app, configurable backends, containerizable)
- **Kafka-Ready** (Optional async messaging via RabbitMQ or Kafka)
- **SOLID Principles** (Maintainability, testability, extensibility)

## 📁 Template Structure

```
cloud-ddd-template/
├── README.md                    # Overview and key features
├── CLAUDE.md                    # Detailed architecture guidelines
├── GETTING_STARTED.md          # Step-by-step setup guide
├── scripts/
│   └── create-project.ps1      # Project scaffolding script
├── src/
│   ├── Templates.Domain/        # Domain logic (no dependencies)
│   ├── Templates.Application/   # Use cases, handlers
│   ├── Templates.Infrastructure/ # Data access, external services
│   └── Templates.Api/           # HTTP endpoints, middleware
└── tests/
    ├── Templates.UnitTests/     # Domain & application tests
    └── Templates.IntegrationTests/ # API & end-to-end tests
```

## ✨ Key Features

### Essential Cerberus Integration (Always Included)
- ✅ **SharedCommon.Core** — `Result<T>`, `Guard`, domain exceptions
- ✅ **SharedCommon.Logging** — Structured Serilog logging
- ✅ **SharedCommon.Middlewares** — CorrelationId, exception handling, request logging
- ✅ **SharedCommon.ResponseBuilder** — Standardized API responses
- ✅ **SharedCommon.HealthChecks** — Service health checks
- ✅ **SharedCommon.Validation** — FluentValidation with DI
- ✅ **SharedCommon.Observability** — OpenTelemetry tracing and metrics

### Optional Cerberus Packages (Config-Driven)
- 🔧 **SharedCommon.Caching** — Hybrid L1 (in-memory) + L2 (Redis) caching
- 🔧 **SharedCommon.Messaging** — RabbitMQ or Kafka (switchable)
- 🔧 **SharedCommon.Cloud** — Azure/AWS blob storage, secrets, queues
- 🔧 **SharedCommon.MultiTenancy** — Tenant isolation (SaaS)
- 🔧 **SharedCommon.Auditing** — Audit trail logging
- 🔧 **SharedCommon.BackgroundJobs** — Hangfire job scheduling

### Architecture Best Practices
- ✅ **Layering** — No circular dependencies, no infrastructure leakage
- ✅ **Separation of Concerns** — Each layer has clear responsibility
- ✅ **Testability** — All layers independently testable
- ✅ **DDD** — Domain aggregates, value objects, domain events
- ✅ **Result<T> Pattern** — Expected failures as return values
- ✅ **Async/Await** — All I/O operations async with CancellationToken
- ✅ **Dependency Injection** — All services resolved via container
- ✅ **Configuration-Driven** — No hardcoded values
- ✅ **Health Checks** — Liveness and readiness probes
- ✅ **Structured Logging** — JSON logs with correlation IDs

### Example Feature: Orders
The template includes a complete **Orders** feature demonstrating:
- ✅ Domain aggregate (`Order`) with value objects
- ✅ Domain events (`OrderCreated`, `OrderConfirmed`, `OrderCancelled`)
- ✅ Repository pattern for data access
- ✅ Application command handler with validation
- ✅ HTTP endpoints with OpenAPI documentation
- ✅ Unit tests (domain logic)
- ✅ Integration tests (API endpoints)

## 🚀 Quick Start

### Create a New Project

```powershell
# Windows PowerShell
.\scripts\create-project.ps1 -ProjectName "MyService"

# Or manually copy template and rename
```

### Setup and Run

```bash
cd MyService
dotnet restore
dotnet build

# Configure database
# Edit src/MyService.Api/appsettings.json

# Apply migrations
cd src/MyService.Infrastructure
dotnet ef database update

# Run the API
cd ../MyService.Api
dotnet run

# Visit http://localhost:5000/swagger
```

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| [README.md](./README.md) | Feature overview and benefits |
| [CLAUDE.md](./CLAUDE.md) | Detailed architecture guidelines and patterns |
| [GETTING_STARTED.md](./GETTING_STARTED.md) | Step-by-step setup and first feature walkthrough |

## 🏗️ Architecture Overview

### Domain Layer
- Business logic, no dependencies
- Entities, value objects, aggregates
- Domain services, domain events
- Domain exceptions

### Application Layer
- Use cases, orchestration
- Commands and queries (MediatR)
- Validators (FluentValidation)
- Handler logic

### Infrastructure Layer
- Data persistence (EF Core, repositories)
- External service clients
- Background jobs, caching
- Unit of Work pattern

### API Layer
- HTTP endpoints (Minimal APIs or Controllers)
- Middleware configuration
- Feature grouping (vertical slices)
- OpenAPI documentation

## 🔄 Vertical Slicing Example

Each feature is self-contained:

```
Features/Orders/Create/
├── CreateOrderCommand.cs          # Request
├── CreateOrderCommandHandler.cs   # Business logic
├── CreateOrderValidator.cs        # Input validation
├── CreateOrderEndpoint.cs         # HTTP endpoint
├── CreateOrderResponse.cs         # Response
└── CreateOrderTests.cs            # Tests
```

**Benefits**:
- Cohesion — all code for a feature in one place
- Independence — features can be developed in parallel
- Testability — feature tests in same folder
- Scalability — can be extracted to microservice

## 🧪 Testing Strategy

### Unit Tests
- Domain logic and aggregates
- Application handlers and validators
- No database, all dependencies mocked

### Integration Tests
- API endpoints end-to-end
- In-memory database or test containers
- WebApplicationFactory for full app testing

```bash
dotnet test                          # Run all
dotnet test tests/MyService.UnitTests/
dotnet test tests/MyService.IntegrationTests/
```

## 🔐 Security by Default

- ✅ No secrets in code (appsettings, User Secrets, env vars)
- ✅ Input validation on all endpoints
- ✅ Authorization policies (not role-based)
- ✅ Structured logging excludes PII
- ✅ SQL injection prevention (ORM)
- ✅ CORS configuration by environment
- ✅ HTTP security headers

## ☁️ Cloud Ready

- ✅ **12-Factor App** — Configuration via environment
- ✅ **Containerizable** — Dockerfile-ready
- ✅ **Health Checks** — Liveness and readiness probes
- ✅ **Distributed Tracing** — OpenTelemetry support
- ✅ **Multi-Cloud** — Azure, AWS abstractions
- ✅ **Observable** — Structured logging, metrics, tracing

## 📦 Optional: Kafka Ready

Enable messaging with:
```json
{
  "Features": {
    "Messaging": {
      "Enabled": true,
      "Transport": "Kafka",
      "Kafka": { "BootstrapServers": "localhost:9092" }
    }
  }
}
```

## 🎯 Use Cases

This template is ideal for:
- ✅ Microservices in enterprise environments
- ✅ Cloud-native applications (Azure, AWS)
- ✅ Event-driven systems (Kafka, RabbitMQ)
- ✅ SaaS platforms (multi-tenancy)
- ✅ API-first architectures
- ✅ Domain-complex business logic
- ✅ High-reliability systems

**NOT suitable for:**
- Simple CRUD apps (use scaffolding)
- Proof-of-concepts (might be overkill)
- Monoliths with simple logic

## 🔗 References

- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design by Eric Evans](https://domainlanguage.com/ddd/)
- [Vertical Slice Architecture by Jimmy Bogard](https://jimmybogard.com/vertical-slice-architecture/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Cerberus Platform Documentation](../../../docs/)

## 📋 Checklist for New Project

- [ ] Create project using `create-project.ps1`
- [ ] Update connection string in `appsettings.json`
- [ ] Run `dotnet restore && dotnet build`
- [ ] Run database migrations
- [ ] Start API and verify `/swagger` loads
- [ ] Implement first domain feature (aggregate + value objects)
- [ ] Create repository interface and implementation
- [ ] Create application command handler
- [ ] Create API endpoint
- [ ] Write unit and integration tests
- [ ] Enable optional Cerberus packages as needed
- [ ] Configure CORS for frontend origin
- [ ] Setup logging/tracing (Serilog, OpenTelemetry)
- [ ] Add Docker/Kubernetes deployment

## ⚠️ Important Rules

**NEVER break:**
- ✅ No circular dependencies
- ✅ No business logic in endpoints
- ✅ No infrastructure leakage into domain
- ✅ All I/O async with CancellationToken
- ✅ Result<T> for expected failures
- ✅ Guard clauses for validation
- ✅ XML docs on public APIs
- ✅ Unit tests for domain logic

## 🤝 Contributing

To improve this template:
1. Test in production-like scenario
2. Document any learnings
3. Update CLAUDE.md with guidelines
4. Add example features if needed
5. Submit feedback to Cerberus team

## 📞 Support

- 📖 Read [GETTING_STARTED.md](./GETTING_STARTED.md)
- 📖 Review [CLAUDE.md](./CLAUDE.md)
- 📖 Check [Cerberus docs](../../../docs/)
- 👥 Consult example (Orders feature)

---

**Template Version**: 1.0  
**Compatible With**: Cerberus v3.0+, .NET 8.0+  
**Last Updated**: 2026-05-29  
**Status**: Production-Ready ✅
