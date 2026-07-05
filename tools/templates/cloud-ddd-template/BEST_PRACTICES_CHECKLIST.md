# Cloud-Ready DDD Template — Best Practices Checklist

Essential practices to maintain code quality, security, and performance.

## Architecture & Design

- [ ] **Layer Separation**: No circular dependencies between Domain → Application → Infrastructure → API
- [ ] **Dependency Inversion**: High-level modules don't depend on low-level modules; both depend on abstractions
- [ ] **SOLID Principles Enforced**:
  - [ ] Single Responsibility: Each class has one reason to change
  - [ ] Open/Closed: Open for extension, closed for modification
  - [ ] Liskov Substitution: Derived classes are substitutable for base classes
  - [ ] Interface Segregation: Clients depend only on methods they use
  - [ ] Dependency Inversion: Depend on abstractions, not implementations
- [ ] **No God Objects**: Classes under 200 lines (except generated code)
- [ ] **Aggregate Boundaries**: Clear, well-defined aggregates with single root entity
- [ ] **Value Objects**: Used for conceptual wholes (Money, OrderId, UserId, etc.)
- [ ] **Domain Events**: Published for significant business domain changes
- [ ] **No Infrastructure in Domain**: Domain layer has zero external dependencies
- [ ] **Vertical Slices**: Each feature is self-contained in Features/FeatureName/Operation/ structure
- [ ] **Repository Pattern**: Data access abstracted, not entity framework calls in application layer

## Code Quality

- [ ] **No Compiler Warnings**: `TreatWarningsAsErrors=true` enforced
- [ ] **XML Documentation**: All public API methods documented with `/// <summary>`
- [ ] **Consistent Naming**: 
  - [ ] PascalCase for classes, methods, properties
  - [ ] camelCase for parameters and local variables
  - [ ] CONSTANT_CASE for constants
  - [ ] Interfaces prefixed with `I`
- [ ] **No Magic Numbers**: All constants named and explained
- [ ] **No Dead Code**: Unused namespaces, methods, classes removed
- [ ] **No TODO Comments**: Issues tracked in backlog, not code
- [ ] **Formatting Consistent**: Use `.editorconfig` for automated enforcement
- [ ] **Null Safety**: `#nullable enable` in all files, nullability annotations used
- [ ] **No Hardcoded Values**: Secrets, URLs, ports in configuration
- [ ] **Code Reviews**: All changes reviewed before merging to main
- [ ] **Metrics Tracked**: Cyclomatic complexity ≤ 10 per method

## Testing

- [ ] **Unit Tests**: ≥80% code coverage (critical path ≥95%)
- [ ] **Test Naming**: `[Method]_[Scenario]_[ExpectedResult]` pattern
- [ ] **Arrange-Act-Assert**: Clear structure in all tests
- [ ] **No Test Interdependencies**: Each test runs independently
- [ ] **No Random Data in Tests**: Use explicit values with meaning
- [ ] **Fast Execution**: Unit tests complete in <100ms each
- [ ] **Integration Tests**: End-to-end scenarios with real dependencies
- [ ] **In-Memory Database**: Use `InMemoryDatabase` for integration tests
- [ ] **Mutation Testing**: Consider with Stryker to verify test quality
- [ ] **Performance Tests**: Long-running queries, large datasets tested
- [ ] **Security Tests**: SQL injection, XSS, CSRF vectors covered
- [ ] **Architecture Tests**: Layer separation and dependency rules enforced

## Validation & Error Handling

- [ ] **Guard Clauses First**: Validate inputs at method entry (from `SharedCommon.Core`)
- [ ] **FluentValidation**: All commands/queries validated before handler execution
- [ ] **Domain Exceptions**: Business rule violations throw `DomainException` with code
- [ ] **Result<T> Pattern**: Used consistently for expected failures
- [ ] **Meaningful Error Messages**: Actionable, not cryptic
- [ ] **Error Codes**: Standard codes across API (e.g., `ORD-001`, `AUTH-001`)
- [ ] **No Silent Failures**: Exceptions logged with context
- [ ] **Validation Pipeline**: `ValidationBehavior` intercepts all commands
- [ ] **Request Logging**: All requests logged with method, path, duration, status

## Async & Concurrency

- [ ] **Async All the Way**: No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
- [ ] **CancellationToken**: Passed through all async methods
- [ ] **No Thread.Sleep()**: Use proper async/await
- [ ] **No Blocking Calls**: No `Thread.Create()`, `Parallel.For()` in I/O operations
- [ ] **Database Connection Pooling**: Proper connection string configuration
- [ ] **Thread-Safe Collections**: `ConcurrentDictionary`, `ConcurrentBag` for shared state
- [ ] **Lock Statements Rare**: Design avoids need for explicit locks
- [ ] **Deadlock Prevention**: Consistent lock ordering

## Logging & Monitoring

- [ ] **Structured Logging**: JSON output with Serilog (not formatted strings)
- [ ] **Correlation IDs**: All requests include unique trace ID
- [ ] **Log Levels**: 
  - [ ] Information: Important business events (order created, user logged in)
  - [ ] Warning: Unexpected conditions (failed retry, unusual latency)
  - [ ] Error: Recoverable failures (validation error, timeout)
  - [ ] Fatal: Application cannot continue (database unavailable)
- [ ] **No Sensitive Data**: Passwords, credit cards, PII never logged
- [ ] **Performance Logging**: Long-running operations logged with duration
- [ ] **OpenTelemetry**: Distributed tracing configured and working
- [ ] **Metrics**: Business metrics collected (orders created, API latency)
- [ ] **Health Checks**: `/health/live` and `/health/ready` endpoints functional
- [ ] **Alerts Configured**: Critical thresholds trigger notifications
- [ ] **Centralized Logging**: Logs sent to ELK, Splunk, or Azure Monitor
- [ ] **Log Retention**: Old logs archived according to compliance requirements

## Security

- [ ] **No Secrets in Code**: All secrets externalized to configuration/secrets manager
- [ ] **Secrets Manager**: Azure KeyVault, AWS Secrets Manager, or HashiCorp Vault used
- [ ] **API Authentication**: Bearer token authentication for all non-public endpoints
- [ ] **Authorization**: Role/permission checks enforced at command handler level
- [ ] **Input Validation**: All user input validated (length, format, type)
- [ ] **SQL Injection Prevention**: Parameterized queries (EF Core by default)
- [ ] **XSS Prevention**: No raw HTML output, use proper encoding
- [ ] **CSRF Protection**: Anti-forgery tokens on state-changing operations
- [ ] **Rate Limiting**: API endpoints rate-limited to prevent abuse
- [ ] **Dependency Scanning**: NuGet packages scanned for known vulnerabilities
- [ ] **Security Headers**: HSTS, CSP, X-Frame-Options set appropriately
- [ ] **HTTPS Only**: All traffic encrypted in transit (TLS 1.3+)
- [ ] **Secure Defaults**: Most restrictive security settings by default
- [ ] **Encryption at Rest**: Sensitive data encrypted in database
- [ ] **Access Logs**: All access attempts logged for audit trail
- [ ] **OWASP Top 10**: Common vulnerabilities checked and mitigated

## Database

- [ ] **Migrations Tracked**: All schema changes in migration files
- [ ] **No Raw SQL**: Use EF Core LINQ or stored procedures when necessary
- [ ] **Indices Optimized**: Frequently queried columns have indices
- [ ] **Foreign Keys**: Referential integrity enforced at database level
- [ ] **Seed Data**: Initial data provided in migrations, not hardcoded
- [ ] **Connection Pooling**: `Min Pool Size` and `Max Pool Size` configured appropriately
- [ ] **Query Performance**: Slow queries (>100ms) investigated and optimized
- [ ] **N+1 Queries**: `.Include()` used appropriately, no lazy loading
- [ ] **Pagination**: Large result sets paginated, never return all records
- [ ] **Backup Strategy**: Automated backups tested for recoverability
- [ ] **Read Replicas**: High-read workloads use read replicas
- [ ] **Transactions**: Long-running transactions avoided (keep under 5 seconds)
- [ ] **Cascade Deletes**: Explicitly configured, never automatic
- [ ] **Column Names**: Snake_case for columns (matches C# PascalCase via EF)

## Configuration

- [ ] **12-Factor App Compliance**: Configuration via environment variables
- [ ] **Environment Files**: `.env.local` ignored in git, secrets not committed
- [ ] **Configuration Classes**: Strong-typed `IOptions<T>` pattern used
- [ ] **Validation**: Configuration values validated on startup (fail fast)
- [ ] **Secrets Rotation**: Plan for secret rotation without downtime
- [ ] **Feature Flags**: Toggle features without code deployment
- [ ] **Environment-Specific Settings**: Development, Staging, Production configs
- [ ] **Defaults Reasonable**: Application runs with default config (except secrets)
- [ ] **Documentation**: All configuration options documented with examples

## Performance

- [ ] **Response Compression**: Gzip compression enabled for JSON responses
- [ ] **Caching Strategy**: L1 (in-memory) + L2 (Redis) hybrid configured
- [ ] **Cache Invalidation**: Clear strategy for cache eviction/invalidation
- [ ] **Async I/O**: No blocking database or HTTP calls
- [ ] **Pagination**: Large datasets paginated with reasonable defaults (25 items)
- [ ] **Projection**: Select only needed columns (`.Select()` in queries)
- [ ] **Batch Operations**: Bulk insert/update used for large datasets
- [ ] **Connection Pooling**: Database connections pooled and reused
- [ ] **Lazy Loading Disabled**: Prevent accidental N+1 queries
- [ ] **Memory Usage**: Applications under 512MB base memory
- [ ] **CPU Utilization**: Single-threaded tests ≤10% CPU (unless intentional)
- [ ] **Startup Time**: Application fully ready in ≤15 seconds
- [ ] **Request Latency**: P99 latency <500ms for typical requests
- [ ] **Throughput**: Support target QPS without queuing
- [ ] **Load Testing**: Performance tested under expected load (load test results documented)

## Deployment & Operations

- [ ] **Containerized**: Dockerfile present, images build successfully
- [ ] **Health Checks**: Liveness and readiness probes responding
- [ ] **Graceful Shutdown**: Application completes in-flight requests before shutdown
- [ ] **Structured Logging**: All logs structured JSON (not plain text)
- [ ] **Metrics Exported**: Prometheus-compatible `/metrics` endpoint
- [ ] **Distributed Tracing**: OpenTelemetry traces exported
- [ ] **Version Pinning**: All dependencies pinned to specific versions (no floating versions)
- [ ] **Update Strategy**: Regular dependency updates (monthly) with security patches (immediate)
- [ ] **Backward Compatibility**: API changes backward-compatible or versioned
- [ ] **Database Migrations**: Backward-compatible, no data loss
- [ ] **Runbooks**: Documentation for common operational tasks
- [ ] **Incident Response**: Plan for service outages with escalation
- [ ] **Rollback Plan**: Clear procedure to rollback to previous version
- [ ] **Zero-Downtime Deployments**: Rolling updates without service interruption
- [ ] **Backup Tested**: Backups created and tested for recoverability monthly
- [ ] **Disaster Recovery**: RTO/RPO defined and tested

## Documentation

- [ ] **README**: Clear overview of what the service does
- [ ] **Architecture Diagram**: Visual representation of components and data flow
- [ ] **Database Schema**: ER diagram or documented in migrations
- [ ] **API Documentation**: OpenAPI/Swagger with all endpoints documented
- [ ] **Configuration Guide**: All configurable options listed with examples
- [ ] **Deployment Guide**: Step-by-step instructions for each environment
- [ ] **Troubleshooting Guide**: Common issues and solutions
- [ ] **Contributing Guide**: How to run locally, test, and submit changes
- [ ] **CHANGELOG**: Notable changes documented for each release
- [ ] **ADR (Architecture Decision Records)**: Major decisions recorded with rationale
- [ ] **Code Examples**: Sample requests/responses for API endpoints
- [ ] **Security Considerations**: Authentication, authorization, encryption documented
- [ ] **Performance Characteristics**: Expected latency, throughput, scalability limits
- [ ] **Dependencies**: Third-party libraries documented with versions
- [ ] **Maintenance Plan**: Expected lifespan, support timeline, upgrade path

## DevOps & Infrastructure

- [ ] **Infrastructure as Code**: All infrastructure defined in code (Terraform, ARM, CloudFormation)
- [ ] **Environment Consistency**: Dev/Staging/Production environments identical (except scale)
- [ ] **Automated Deployments**: CI/CD pipeline (GitHub Actions, GitLab CI, Azure Pipelines)
- [ ] **Blue-Green Deployments**: Zero-downtime deployments possible
- [ ] **Canary Deployments**: New versions tested with small user percentage first
- [ ] **Monitoring Alerts**: Key metrics trigger notifications (error rate, latency, CPU)
- [ ] **Log Aggregation**: Centralized logging across all instances
- [ ] **Distributed Tracing**: Request tracing across services
- [ ] **SLA Defined**: Uptime and performance targets documented
- [ ] **Capacity Planning**: Growth projected and infrastructure scaled accordingly
- [ ] **Cost Monitoring**: Expenses tracked and optimized (reserved instances, spot instances)
- [ ] **Disaster Recovery**: Data replicated to another region/account
- [ ] **Compliance**: Audit logs for regulatory requirements (HIPAA, GDPR, PCI-DSS)

## Git & Version Control

- [ ] **Main Branch Protected**: Requires code review before merging
- [ ] **Feature Branches**: Created from latest main, named descriptively
- [ ] **Commit Messages**: Clear, descriptive messages (not "fix bug" or "wip")
- [ ] **Squash Commits**: Clean history, one logical change per commit
- [ ] **No Merge Conflicts**: Regularly rebased on main during development
- [ ] **Branch Deletions**: Old branches cleaned up after merge
- [ ] **Tag Releases**: Version tags created for releases
- [ ] **Secrets Scanning**: Git history scanned for accidentally committed secrets
- [ ] **Large Files Excluded**: `.gitignore` prevents committing large files
- [ ] **Consistent Formatting**: `.editorconfig` ensures code style consistency

## Team & Process

- [ ] **Code Review Process**: All changes reviewed (min 1 approver)
- [ ] **Review Quality**: Reviewers check logic, tests, security, performance
- [ ] **PR Templates**: Pull request template guides authors
- [ ] **Onboarding Guide**: New developers can run locally in <30 minutes
- [ ] **Pair Programming**: Complex features pair-programmed
- [ ] **Knowledge Sharing**: Team meetings discuss architectural decisions
- [ ] **Tech Debt Management**: Time allocated for refactoring (20% of sprint)
- [ ] **Incident Postmortems**: Learning from outages (blameless culture)
- [ ] **Design Reviews**: Architecture changes discussed before implementation
- [ ] **Standards Documentation**: Team standards documented (not implicit)

---

## Verification Checklist Before Release

**Day Before Release:**
- [ ] All tests passing (unit, integration, performance, security)
- [ ] Code coverage at target level
- [ ] No compiler warnings
- [ ] Staging environment fully tested
- [ ] Database migrations tested (forward and backward)
- [ ] Rollback plan documented and tested
- [ ] Configuration validated for production
- [ ] Documentation updated (README, CHANGELOG, deployment guide)
- [ ] Security scan completed (no high/critical vulnerabilities)
- [ ] Performance benchmarks meet targets
- [ ] Alert thresholds configured and tested
- [ ] On-call team notified of release

**Release Day:**
- [ ] Monitoring dashboards active
- [ ] Log aggregation verified
- [ ] Trace collection working
- [ ] Backup created
- [ ] Canary deployment successful (if applicable)
- [ ] Progressive rollout to 10%, 50%, 100% traffic
- [ ] Health checks passing on all instances
- [ ] Error rate normal
- [ ] Latency normal
- [ ] Database performance normal
- [ ] No unexpected alerts
- [ ] Team available for immediate response

**Post-Release (24 hours):**
- [ ] Monitor error rates (should be ≤0.1%)
- [ ] Monitor latency (P99 <500ms)
- [ ] Monitor resource usage (CPU <70%, Memory <80%)
- [ ] No escalations from customers
- [ ] All features working as expected
- [ ] Performance meets or exceeds targets
- [ ] No memory leaks (monitor memory trend)
- [ ] Rollback not needed

---

**Maintain These Standards Throughout the Project Lifetime**

These practices prevent technical debt, ensure security, maintain performance, and enable scaling.

**Last Updated**: 2026-05-30
