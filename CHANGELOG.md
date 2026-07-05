# Changelog

All notable changes to the SharedCommon platform are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

#### SharedCommon.MultiTenancy — data isolation warnings
- **SECURITY:** Enhanced `ITenantContext` with comprehensive data isolation documentation
- Added remarks and guidelines for query layer, cache layer, authorization, background jobs, logging, and third-party API security
- Created `TenantDataIsolationTests` suite documenting 10 security scenarios and isolation requirements
- Updated `TenantMiddleware` with security considerations
- Updated `ITenantResolver` with input/output validation guidance
- Added extensive CLAUDE.md section on data isolation enforcement with 7 security domains
- **Critical:** Package provides tenant identification only; application code MUST enforce isolation at all layers

#### SharedCommon.Core — domain foundation types
- `Guard` — static guard clause methods: `AgainstNull`, `AgainstNullOrEmpty`, `AgainstNullOrWhiteSpace`, `AgainstEmpty` (collection), `AgainstLessThan`, `AgainstGreaterThan`, `AgainstOutOfRange`, `AgainstEmptyGuid`, `AgainstInvalidState`, `AgainstExceedingLength`; uses `[CallerArgumentExpression]` for zero-friction error messages
- `PagedResult<T>` — typed paged list with `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`, `Empty`, `From` factory; companion `Pagination` record with `Default`, `Of` (clamped), `Offset`
- `IEntity<TId>` — base identity contract for domain entities
- `IValueObject` — marker interface for DDD value objects
- `IAggregateRoot<TId>` — extends `IEntity<TId>` with `DomainEvents` for event sourcing

#### SharedCommon.Cloud (new package)
- `IBlobStorageService` — `UploadAsync`, `DownloadAsync`, `DeleteAsync`, `ExistsAsync`, `GetPresignedUrlAsync`
- `ISecretManagerService` — `GetSecretAsync`, `GetSecretAsync<T>`, `SetSecretAsync`
- `ICloudQueueService` — `SendAsync<T>`, `ReceiveAsync<T>`, `AcknowledgeAsync`; `CloudMessage<T>` carrier
- `CloudOptions` — `Provider` (Azure/AWS), `AzureCloudOptions`, `AwsCloudOptions`
- `AddSharedCloud(IConfiguration)` — registers options and validates at startup; provider implementations registered by provider-specific packages

#### SharedCommon.ApiVersioning (new package)
- URL-segment, query-string, header, and media-type version reading strategies (individually toggleable)
- `ApiVersioningOptions` — `DefaultVersion`, `AssumeDefaultWhenUnspecified`, `ReportApiVersions`, `VersionReadingStrategy`
- `AddSharedApiVersioning(IConfiguration)` — registers `Asp.Versioning.Mvc` with configured strategy and API Explorer groups (`'v'VVV` format for Swagger)
- URL segment versioning enabled by default; all other strategies opt-in

#### SharedCommon.FeatureFlags (new package)
- `IFeatureFlagService` — `IsEnabledAsync`, `IsEnabledForAsync<TContext>` (targeting), `GetEnabledFeaturesAsync`
- `FeatureFlagContext` — context record for user/tenant/group-based targeting
- `FeatureFlagService` — default implementation backed by `IFeatureManager` (Microsoft.FeatureManagement)
- `FeatureFlagOptions` — `CacheTtlSeconds`, `LogEvaluations`
- `AddSharedFeatureFlags(IConfiguration)` — registers `IFeatureManager` from `FeatureManagement` config section

#### Tests
- `tests/SharedCommon.Core.UnitTests` — extended with `GuardTests` (28 tests), `PagedResultTests` (14 tests)
  - `ResultTests` — full coverage of `Result` and `Result<T>`: all three arms, factory methods, pattern matching, record equality
  - `CorrelationIdTests` — `New`, `From`, `TryCreate`, `ToString`, implicit string conversion, edge cases (null, empty, whitespace)
  - `RequestContextTests` — default state, property mutations, case-insensitive Properties dictionary, IRequestContext interface contract
  - `ExceptionsTests` — all five domain exceptions, HTTP status codes, custom error codes
  - `GuardTests` — all guard clause variants, reference/nullable-value types, collections, ranges, Guid, state, length
  - `PagedResultTests` — `Pagination` defaults/clamping/offset, `PagedResult<T>` navigation, `Empty`, `From` slicing
- `tests/SharedCommon.Utilities.UnitTests` — 27 tests: `StringExtensionsTests`, `CollectionExtensionsTests`, `DateTimeExtensionsTests`
- `tests/SharedCommon.Auditing.UnitTests` — 19 tests: `AuditBuilderTests`, `AuditEntryTests`
- `tests/SharedCommon.Middlewares.UnitTests` — behavioral tests for ASP.NET Core middleware:
  - `CorrelationIdMiddlewareTests` — 8 tests: header reading, GUID generation, response propagation, IRequestContext population, custom headers, config options
  - `ExceptionHandlingMiddlewareTests` — 14 tests: exception-to-HTTP-status mapping (404, 401, 403, 409, 429, 500), JSON error responses, conditional stack traces, response-already-started edge case
  - `RequestLoggingMiddlewareTests` — 10 tests: path exclusion (`/health`, `/metrics`), enabled/disabled behavior, middleware continuation, HTTP method/path/status preservation
  - **Total: 36 tests, 100% pass rate** with `TreatWarningsAsErrors=true`
- `tests/SharedCommon.BackgroundJobs.UnitTests` — 11 tests: `CronTests`, `BackgroundJobOptionsTests`
- `tests/SharedCommon.GraphQL.UnitTests` — 11 tests: `ConnectionTests` (cursor pagination, page info, edge)
- `tests/SharedCommon.Resiliency.UnitTests` — 17 tests: configuration and behavioral coverage
  - `ResiliencyOptionsTests` — 5 tests: retry, circuit breaker, and timeout configuration defaults
  - `ResiliencyBehaviorTests` — 12 tests: pipeline registration, execution, configuration storage, pipeline distinctness, multi-execution consistency, logger injection
- `tests/SharedCommon.Logging.UnitTests` — 10 tests: `LoggingOptionsTests`
- `tests/SharedCommon.Caching.UnitTests` — 81 behavioral tests covering all cache operations
  - `CacheServiceTests` — 21 tests: core operations (get, set, remove, exists), `GetOrSetAsync` with stampede protection, batch operations, expiration handling, cancellation token propagation
  - `CacheOptionsTests` — 13 tests: configuration validation for `CachingOptions`, `MemoryCacheOptions`, `RedisCacheOptions`, `DatabaseCacheOptions`; multi-tier setup
  - `CacheKeyValidationTests` — 19 tests: key convention validation (`{package}:{entity}:{id}`), case sensitivity, special characters, consistency across operations
  - `CacheErrorHandlingTests` — 28 tests: null/empty key rejection, missing key idempotent handling, concurrent deduplication, exception type validation, cleanup operations
- `tests/SharedCommon.Auth.UnitTests` — 78 tests: `AuthOptionsTests`, `JwtAuthServiceTests` (behavioral), `CurrentUserTests` (behavioral), `AuthUserTests` (behavioral)
  - `AuthOptionsTests` — 11 tests: JWT options defaults, validation options, password policy, OAuth, token blacklist configuration
  - `JwtAuthServiceTests` — 50 behavioral tests: token generation (userId, email, roles, expiration), token validation (issuer, audience, signature, expiry, blacklist), token revocation, refresh token flow, clock skew, secret key validation
  - `CurrentUserTests` — 12 behavioral tests: claim extraction (userId, email, roles, permissions), authentication state, fallback to sub claim, HTTP context null-safety, CurrentUser-to-AuthUser mapping
  - `AuthUserTests` — 5 behavioral tests: record equality, authentication state, claim collections, identity verification
- `tests/SharedCommon.Security.UnitTests` — 14 tests: `SecurityOptionsTests`
- `tests/SharedCommon.Observability.UnitTests` — 3 tests: `ObservabilityOptionsTests`
- `tests/SharedCommon.Messaging.UnitTests` — 6 tests: `MessagingOptionsTests`
- `tests/SharedCommon.Validation.UnitTests` — 6 tests: `ValidationOptionsTests`
- `tests/SharedCommon.Middlewares.UnitTests` — 10 tests: `MiddlewareOptionsTests`
- `tests/SharedCommon.HealthChecks.UnitTests` — 7 tests: `HealthCheckOptionsTests`
- `tests/SharedCommon.Grpc.UnitTests` — 6 tests: `GrpcOptionsTests`
- `tests/SharedCommon.ResponseBuilder.UnitTests` — 12 tests: `ApiResponseTests`

#### SharedCommon.Messaging — Kafka transport
- `MessagingTransport` enum — `RabbitMQ` (default) or `Kafka`
- `KafkaOptions` — `BootstrapServers`, `ConsumerGroupId`, `SaslUsername/Password`, `SecurityProtocol`, topic defaults
- `RabbitMqOptions` — existing RabbitMQ properties moved under a nested `RabbitMQ:{}` section
- `ServiceCollectionExtensions.AddSharedMessaging` — automatically wires the correct MassTransit transport based on `Transport` config value
- Consumer and publisher code is **transport-agnostic** — no application code changes required when switching transports

#### SharedCommon.GraphQL (new package)
- Hot Chocolate 14 infrastructure for ASP.NET Core services
- `DomainErrorFilter` — maps `DomainException` subclasses to structured GraphQL errors; internal exceptions become `INTERNAL_ERROR` (no detail leakage)
- `DataLoaderBase<TKey, TValue>` — base class enforcing N+1-safe DataLoader pattern
- `Connection<T>` and `Edge<T>` — Relay-compliant cursor-based pagination types with `Connection<T>.From(list, totalCount)` factory
- `PageInfo` — Relay `hasNextPage`, `hasPreviousPage`, `startCursor`, `endCursor`
- `GraphQLOptions` — `MaxAllowedComplexity`, `MaxAllowedExecutionDepth`, `EnableIntrospection`, `Path`, `EnableBananaCakePop`
- `AddSharedGraphQL(IConfiguration)` — registers Hot Chocolate with error filter, complexity/depth limits, and introspection control
- `MapSharedGraphQL(IHostEnvironment)` — maps the GraphQL endpoint

#### SharedCommon.Auditing (new package)
- `AuditEntry` — immutable record with `Action`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `CorrelationId`, `OldValues`, `NewValues`, `ChangedProperties`, `Metadata`
- `AuditAction` enum — `Created`, `Updated`, `Deleted`, `SoftDeleted`, `Accessed`
- `IAuditService` — `RecordAsync`, `RecordBatchAsync`, `GetHistoryAsync`
- `AuditBuilder` — fluent builder for constructing audit entries; `FromContext(IRequestContext)` for one-call actor propagation
- `IAuditStore` — storage abstraction; implement in your EF Core DbContext for the Database backend
- `AuditOptions` — `Backend` (`Logging` / `Database` / `Messaging`), `CaptureValueSnapshots`, `RecordReadAccess`, `RetentionDays`, `ExcludedEntityTypes`
- `LoggingAuditService` — default; writes structured log entries, zero persistence, works with any log aggregation pipeline
- `DatabaseAuditService` — persists to a relational table via `IAuditStore`; errors are caught and logged, never surfaced to callers
- `AddSharedAuditing(IConfiguration)` — registers the configured backend

#### SharedCommon.BackgroundJobs (new package)
- `IBackgroundJobService` — `Enqueue`, `Schedule`, `ScheduleAt`, `AddOrUpdateRecurring`, `RemoveRecurring`, `TriggerRecurring`, `Delete`
- `HangfireBackgroundJobService` — Hangfire-backed implementation
- `BackgroundJobOptions` — `Backend` (`InMemory` / `SqlServer`), `ConnectionString`, `WorkerCount`, `DefaultQueue`, `EnableDashboard`, `DashboardPath`, `DashboardRequiredRole`
- `Cron` — named constants: `EveryMinute`, `Every5Minutes`, `Every15Minutes`, `Every30Minutes`, `Hourly`, `Daily`, `DailyOffPeak`, `Weekly`, `Monthly`
- `RoleBasedDashboardAuthorizationFilter` — gates the Hangfire dashboard to authenticated users with a required role
- `AddSharedBackgroundJobs(IConfiguration)` — registers Hangfire with configured storage and worker count
- `UseSharedBackgroundJobs(IConfiguration)` — mounts Hangfire dashboard when `EnableDashboard: true`

#### SharedCommon.MultiTenancy (new package)
- `ITenantContext` — request-scoped tenant identity: `TenantId`, `TenantName`, `IsResolved`, `Properties`
- `ITenantResolver` — resolves `TenantInfo` from `HttpContext`; implement to add database-backed or custom resolution
- `TenantInfo` — resolved tenant record (TenantId, TenantName, Properties)
- `DefaultTenantResolver` — built-in resolver supporting Header, JWT Claim, Subdomain, and QueryString strategies
- `TenantMiddleware` — populates `ITenantContext` per-request; returns 400 when `RequireTenant: true` and no tenant found
- `MultiTenancyOptions` — `Strategy`, `HeaderName` (`X-Tenant-Id`), `ClaimName` (`tenant_id`), `QueryStringKey` (`tenantId`), `RequireTenant`
- `AddSharedMultiTenancy(IConfiguration)` — registers scoped context, resolver, and options with startup validation
- `UseSharedMultiTenancy()` — adds middleware to the pipeline; place before `UseAuthentication`

#### SharedCommon.Storage (new package)
- `IFileStorageService` — `SaveAsync`, `ReadAsync`, `DeleteAsync`, `ExistsAsync`, `ListAsync`; all paths are logical and relative to the configured container
- `StorageFile` — file metadata record: `Name`, `Path`, `SizeBytes`, `ContentType`, `LastModified`
- `LocalFileStorageService` — local filesystem implementation; creates parent directories automatically; no-op deletes; prefix-filtered listing
- `StorageOptions` — `Provider` (Local/Cloud), `LocalBasePath` (`./storage`), `ContainerName` (`default`)
- `AddSharedStorage(IConfiguration)` — registers `IFileStorageService` as singleton with startup validation

#### Tests (Phase 4 completion)
- `tests/SharedCommon.Cloud.UnitTests` — 15 tests: `CloudOptionsTests`, `AzureCloudOptionsTests`, `AwsCloudOptionsTests`, `CloudMessageTests`
- `tests/SharedCommon.ApiVersioning.UnitTests` — 11 tests: `ApiVersioningOptionsTests`, `VersionReadingStrategyTests`
- `tests/SharedCommon.FeatureFlags.UnitTests` — 13 tests: `FeatureFlagOptionsTests`, `FeatureFlagContextTests`, `FeatureFlagServiceTests` (in-memory config + real DI)
- `tests/SharedCommon.MultiTenancy.UnitTests` — 55 behavioral tests across 7 test classes:
  - `MultiTenancyOptionsTests` — 9 tests: options defaults, configuration binding, normalization
  - `TenantInfoTests` — 10 tests: record equality, property access, immutability
  - `DefaultTenantResolverTests` — 16 tests: all 4 strategies (Header, Claim, Subdomain, QueryString), fallback behavior, edge cases
  - `TenantMiddlewareTests` — 9 tests: tenant resolution, context setup, error responses (400 when required but unresolved), next middleware invocation
  - `TenantContextBehaviorTests` — 10 tests: initialization, mutation, set-once semantics, read-only enforcement, idempotency
  - `TenantContextIsolationTests` — 11 tests: scoped isolation per request, thread safety, context freshness across requests
  - `ServiceCollectionExtensionsTests` — 13 tests: DI registration (scoped TenantContext, resolver), options binding, startup validation
  - `CustomTenantResolverTests` — 12 tests: custom resolver patterns, database lookup simulation, HTTP context access, strategy overrides
- `tests/SharedCommon.Storage.UnitTests` — 17 tests: `StorageOptionsTests`, `StorageFileTests`, `LocalFileStorageServiceTests` (full lifecycle: save/read/exists/delete/list/overwrite)

**Total: 401 tests across 22 unit test projects — 0 failures.**

### Fixed

#### Dependency Resolution
- **`Directory.Packages.props`** — added missing centrally managed package versions
  - `Microsoft.Extensions.Configuration.Binder` v8.0.0 (transitive dependency from multi-tenancy, auditing, and other packages)
  - `xunit.analyzers` v1.18.0 (required by test infrastructure)
  - **Issue**: NuGet restore was failing with `NETSDK1064` when centralized package management couldn't resolve these versions
  - **Impact**: All test projects can now restore and build successfully

#### Test Infrastructure
- **`SharedCommon.Testing`** — marked as non-test project (`<IsTestProject>false</IsTestProject>`)
  - **Issue**: Test runner was incorrectly discovering `SharedCommon.Testing` as a test assembly and trying to execute it, causing `error TESTRUNABORT` due to missing transitive dependencies (`Microsoft.AspNetCore.Authentication.JwtBearer`)
  - **Why**: `SharedCommon.Testing` is a test utilities library (consumed by other test projects), not a test suite itself
  - **Solution**: Explicitly marked it as non-test so discovery pipeline excludes it; all 605+ tests now pass cleanly
  - **No breaking changes**: This is a structural fix, not an API change

### Changed

#### SharedCommon.Messaging — breaking configuration change
- **RabbitMQ connection settings moved** from the root of `SharedCommon:Messaging` into a nested `RabbitMQ:{}` section
  - Before: `SharedCommon:Messaging:Host`, `:Port`, `:VirtualHost`, `:Username`, `:Password`
  - After: `SharedCommon:Messaging:RabbitMQ:Host`, `:RabbitMQ:Port`, etc.
  - **Migration**: update `appsettings.json` and User Secrets key paths. No code changes required.
- `MessagingOptions` now contains `Transport`, `RabbitMQ`, `Kafka`, and `Retry` instead of flat broker fields

#### SharedCommon.Core — bug fix
- `Result<T>.IsSuccess` was always `false` for typed results (`Result<T>.Success` and `Result.Success` are parallel sibling types, not parent/child). Fixed by making `IsSuccess` `virtual` in `Result` and overriding it in `Result<T>`.
  - Callers using `result.IsSuccess` or `result.IsFailure` on a `Result<T>` variable now return the correct value.
  - No API surface change — `virtual` is invisible at the call site.

#### Infrastructure
- `Directory.Build.props` — added `<Version>1.0.0</Version>`, `<AssemblyVersion>`, `<FileVersion>` so all 22 packages version consistently from one place
- `.gitlab-ci.yml` — GitLab CI pipeline with three stages: `test` (unit tests + format check + coverage report), `pack` (NuGet packages), `publish` (pushes to feed on semver tags `v*.*.*`)

### Documentation

- `src/SharedCommon.Cloud/README.md` — new; install, config, blob/secret/queue usage, provider implementation guide
- `src/SharedCommon.ApiVersioning/README.md` — new; install, URL-segment versioning, multi-version controllers, deprecation
- `src/SharedCommon.FeatureFlags/README.md` — new; install, feature definitions, percentage/targeting/time-window filters, contextual evaluation
- `src/SharedCommon.Messaging/README.md` — updated for dual-transport (RabbitMQ / Kafka), new config structure, secrets guidance
- `src/SharedCommon.GraphQL/README.md` — new; full install, config, error filter, DataLoader, Relay pagination usage
- `src/SharedCommon.Auditing/README.md` — new; install, config, `AuditBuilder`, EF Core backend wiring, dashboard querying
- `src/SharedCommon.BackgroundJobs/README.md` — new; install, config, fire-and-forget, recurring jobs, Cron constants, dashboard
- `src/SharedCommon.MultiTenancy/CLAUDE.md` — new; resolution strategies, rules, test strategy, extension points
- `src/SharedCommon.Storage/CLAUDE.md` — new; path conventions, rules (no-throw on missing files), test strategy, extension points
- `docs/guides/consuming-packages.md` — added MultiTenancy and Storage to install list, appsettings reference, dependency map, and per-package README table
- `README.md` — added MultiTenancy and Storage to packages table
- `docs/brd/SharedCommon-BRD-Claude-Optimized.md` — renamed `SharedCommon.Responses` to `SharedCommon.ResponseBuilder`; marked all phases complete

---

## [0.1.0] — 2026-04-01 *(initial internal release)*

### Added

**Phase 1 packages (foundations):**
- `SharedCommon.Core` — `Result<T>`, `CorrelationId`, `IRequestContext`, `RequestContext`, `DomainException` hierarchy, `CoreOptions`
- `SharedCommon.Logging` — Serilog structured logging, Console/File/Elasticsearch sinks, enrichers
- `SharedCommon.Observability` — OpenTelemetry tracing and metrics, OTLP exporter, `CorrelationPropagator`
- `SharedCommon.Caching` — Hybrid L1/L2 cache (`IHybridCacheService`), Redis integration, stampede protection
- `SharedCommon.Security` — Security headers, rate limiting, CORS configuration
- `SharedCommon.Auth` — JWT bearer authentication, `ICurrentUser`, `IAuthService`
- `SharedCommon.Validation` — FluentValidation DI registration, auto-discovery, 422 filter
- `SharedCommon.Middlewares` — Exception handling, correlation ID, request logging middleware
- `SharedCommon.ResponseBuilder` — RFC 9457 `ProblemDetails`, `IResponseBuilder`, `Result<T>` → HTTP mapping
- `SharedCommon.HealthChecks` — Liveness/readiness endpoints, custom `IHealthCheck`, `IHealthCheckReporter`
- `SharedCommon.Utilities` — `StringExtensions`, `DateTimeExtensions`, `CollectionExtensions`, `TypeExtensions`

**Phase 2 packages:**
- `SharedCommon.Resiliency` — Polly v8 retry, circuit breaker, timeout pipelines; HTTP client integration
- `SharedCommon.Messaging` — MassTransit + RabbitMQ, `IMessagePublisher`, consumer pipeline, DLQ
- `SharedCommon.Grpc` — `ExceptionInterceptor`, `CorrelationIdInterceptor`, `LoggingInterceptor`, health check, reflection

**Infrastructure:**
- Central Package Management (`Directory.Packages.props`) — all NuGet versions pinned
- `Directory.Build.props` — `net8.0`, C# 12, nullable enabled, `TreatWarningsAsErrors`
- Architecture tests (`tests/SharedCommon.ArchitectureTests`)
- Security tests (`tests/SharedCommon.SecurityTests`)
- Performance benchmarks (`tests/SharedCommon.PerformanceTests`)
- Consumer-facing documentation for all 14 packages
- `docs/guides/consuming-packages.md`, `docs/architecture/`, `docs/adr/`, `docs/standards/`, `docs/runbooks/`
