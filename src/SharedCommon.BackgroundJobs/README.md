# SharedCommon.BackgroundJobs

Background job infrastructure via Hangfire: fire-and-forget, delayed, scheduled, and recurring jobs with DI integration, structured logging, and role-based dashboard access. Supports in-memory (development) and SQL Server (production) storage.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.BackgroundJobs
```

## Registration

```csharp
// Services
builder.Services.AddSharedBackgroundJobs(builder.Configuration);

// Middleware pipeline (after UseAuthentication + UseAuthorization)
app.UseSharedBackgroundJobs(app.Configuration);
```

## Configuration

```json
{
  "SharedCommon": {
    "BackgroundJobs": {
      "Backend": "SqlServer",
      "WorkerCount": 10,
      "DefaultQueue": "default",
      "EnableDashboard": true,
      "DashboardPath": "/jobs",
      "DashboardRequiredRole": "admin"
    }
  }
}
```

> **Connection string** must never be in `appsettings.json`. Use User Secrets or your secrets manager:
> ```bash
> dotnet user-secrets set "SharedCommon:BackgroundJobs:ConnectionString" "Server=...;Database=jobs"
> ```

| Property | Default | Notes |
|----------|---------|-------|
| `Backend` | `InMemory` | `InMemory` (dev) or `SqlServer` (prod). Jobs are lost on restart with `InMemory`. |
| `WorkerCount` | `5` | Number of Hangfire worker threads. |
| `DefaultQueue` | `default` | Queue name for jobs without an explicit queue. |
| `EnableDashboard` | `false` | Expose the Hangfire dashboard. Always secure with `DashboardRequiredRole`. |
| `DashboardPath` | `/jobs` | Route for the dashboard. |
| `DashboardRequiredRole` | `""` (none) | Empty = no auth check. Set a role in production. |

---

## Scheduling Jobs

Inject `IBackgroundJobService` into any service:

```csharp
public class OrderService(IBackgroundJobService jobs)
{
    public async Task CreateAsync(Order order, CancellationToken ct)
    {
        await _repo.CreateAsync(order, ct);

        // Fire-and-forget — runs as soon as a worker is free
        jobs.Enqueue<IEmailService>(svc => svc.SendOrderConfirmationAsync(order.Id));

        // Delayed — runs after 24 hours
        jobs.Schedule<IOrderService>(
            svc => svc.CheckFulfillmentAsync(order.Id),
            TimeSpan.FromHours(24));

        // Scheduled at a specific time
        jobs.ScheduleAt<IReportService>(
            svc => svc.GenerateMonthlyAsync(order.CustomerId),
            new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month + 1, 1, 0, 0, 0, TimeSpan.Zero));
    }
}
```

## Recurring Jobs

Use `AddOrUpdateRecurring` with a Cron expression. The `Cron` class provides named constants for common schedules.

```csharp
public class AppStartup(IBackgroundJobService jobs)
{
    public void RegisterRecurringJobs()
    {
        // Daily cleanup at 3 AM UTC
        jobs.AddOrUpdateRecurring<ICleanupService>(
            recurringJobId: "cleanup-expired-sessions",
            methodCall:     svc => svc.CleanExpiredAsync(),
            cronExpression: Cron.DailyOffPeak);

        // Every 15 minutes — check payment status
        jobs.AddOrUpdateRecurring<IPaymentService>(
            recurringJobId: "sync-payment-status",
            methodCall:     svc => svc.SyncPendingAsync(),
            cronExpression: Cron.Every15Minutes);

        // Monthly report — first of each month
        jobs.AddOrUpdateRecurring<IReportService>(
            recurringJobId: "monthly-report",
            methodCall:     svc => svc.GenerateAsync(),
            cronExpression: Cron.Monthly);
    }
}
```

### Cron Constants

| Constant | Expression | Schedule |
|----------|-----------|---------|
| `Cron.EveryMinute` | `* * * * *` | Every minute |
| `Cron.Every5Minutes` | `*/5 * * * *` | Every 5 minutes |
| `Cron.Every15Minutes` | `*/15 * * * *` | Every 15 minutes |
| `Cron.Every30Minutes` | `*/30 * * * *` | Every 30 minutes |
| `Cron.Hourly` | `0 * * * *` | Every hour at :00 |
| `Cron.Daily` | `0 0 * * *` | Every day at midnight UTC |
| `Cron.DailyOffPeak` | `0 3 * * *` | Every day at 3 AM UTC |
| `Cron.Weekly` | `0 0 * * 1` | Every Monday at midnight UTC |
| `Cron.Monthly` | `0 0 1 * *` | First of each month at midnight UTC |

---

## Managing Jobs

```csharp
// Trigger a recurring job immediately (ignores its next scheduled time)
jobs.TriggerRecurring("cleanup-expired-sessions");

// Remove a recurring job
jobs.RemoveRecurring("monthly-report");

// Cancel a queued or scheduled job by ID
bool deleted = jobs.Delete(jobId);
```

---

## Dashboard

When `EnableDashboard: true`, the Hangfire dashboard is available at the configured path:

- View queued, processing, succeeded, and failed jobs
- Retry failed jobs manually
- Delete jobs

Always protect with `DashboardRequiredRole` in production. An empty role disables auth (development only).

---

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `IBackgroundJobService` | Scoped | Schedules jobs via Hangfire. |
| `IBackgroundJobClient` | Scoped | Hangfire's built-in client (also injectable directly). |
| `IRecurringJobManager` | Singleton | Hangfire's recurring job manager. |
| Hangfire server | Hosted service | Worker threads processing the queue. |
