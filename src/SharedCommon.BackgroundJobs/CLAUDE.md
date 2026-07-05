# SharedCommon.BackgroundJobs

Background job scheduling via Hangfire. Supports fire-and-forget, delayed, point-in-time, and recurring jobs with DI, structured logging, and role-gated dashboard. InMemory storage for development; SQL Server for production.

## API Surface

- `IBackgroundJobService` — `Enqueue`, `Schedule`, `ScheduleAt`, `AddOrUpdateRecurring`, `RemoveRecurring`, `TriggerRecurring`, `Delete`
- `Cron` — named constants: `EveryMinute`, `Every5Minutes`, `Every15Minutes`, `Every30Minutes`, `Hourly`, `Daily`, `DailyOffPeak`, `Weekly`, `Monthly`
- `BackgroundJobOptions` — `Backend`, `ConnectionString`, `WorkerCount`, `DefaultQueue`, `EnableDashboard`, `DashboardPath`, `DashboardRequiredRole`
- `AddSharedBackgroundJobs(IConfiguration)` — DI and Hangfire server registration
- `UseSharedBackgroundJobs(IConfiguration)` — mounts dashboard when `EnableDashboard: true`

## Rules

**Must:**
- Use `IBackgroundJobService` for all scheduling — never call Hangfire client directly
- Protect the dashboard with `DashboardRequiredRole` in all non-development environments
- Store `ConnectionString` in User Secrets or a secrets manager — never in `appsettings.json`
- Register `AddOrUpdateRecurring` calls at startup so job schedules survive restarts

**Forbidden:**
- `Backend: InMemory` in production (jobs lost on restart)
- Accessing HTTP request context inside job methods (jobs run outside the request pipeline)
- Long-running blocking work inside jobs without CancellationToken support

## Design Decisions

`HangfireBackgroundJobService` delegates to Hangfire's `IBackgroundJobClient` and `IRecurringJobManager`; swapping the underlying scheduler in future would only require a new `IBackgroundJobService` implementation.
The `Cron` class centralises expression strings to avoid typos across services.

## Test Strategy

- Unit test `Cron` constants — each expression against a cron parser
- Unit test `BackgroundJobOptions` defaults and validation
- Integration tests use `InMemory` backend to verify job enqueue → execution flow

## Extension Points

- Additional storage backends (PostgreSQL, MongoDB) via Hangfire provider packages
- Custom `IBackgroundJobService` wrapper for job metadata enrichment or multi-queue routing
