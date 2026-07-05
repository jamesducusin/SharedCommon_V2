using System.Linq.Expressions;

namespace SharedCommon.BackgroundJobs;

/// <summary>
/// Schedules background jobs without coupling callers to Hangfire directly.
///
/// Example:
/// <code>
/// public class OrderService(IBackgroundJobService jobs)
/// {
///     public async Task CreateAsync(Order order, CancellationToken ct)
///     {
///         await _repo.CreateAsync(order, ct);
///
///         // Send confirmation email in the background, do not block the request
///         jobs.Enqueue&lt;IEmailService&gt;(svc => svc.SendOrderConfirmationAsync(order.Id));
///
///         // Schedule a follow-up check in 24 hours
///         jobs.Schedule&lt;IOrderService&gt;(
///             svc => svc.CheckFulfillmentAsync(order.Id),
///             TimeSpan.FromHours(24));
///     }
/// }
/// </code>
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a fire-and-forget job on the default queue.
    /// The job executes as soon as a worker thread is available.
    /// </summary>
    /// <typeparam name="T">The service type to resolve from DI when the job runs.</typeparam>
    /// <param name="methodCall">Expression referencing the method to call.</param>
    /// <returns>The Hangfire job ID.</returns>
    string Enqueue<T>(Expression<Action<T>> methodCall) where T : notnull;

    /// <summary>
    /// Enqueues a fire-and-forget job with an async method.
    /// </summary>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall) where T : notnull;

    /// <summary>
    /// Schedules a job to run after a delay.
    /// </summary>
    /// <typeparam name="T">The service type to resolve from DI when the job runs.</typeparam>
    /// <param name="methodCall">Expression referencing the method to call.</param>
    /// <param name="delay">How long to wait before executing.</param>
    /// <returns>The Hangfire job ID.</returns>
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) where T : notnull;

    /// <summary>Schedules an async job to run after a delay.</summary>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) where T : notnull;

    /// <summary>
    /// Schedules a job to run at a specific point in time.
    /// </summary>
    string ScheduleAt<T>(Expression<Action<T>> methodCall, DateTimeOffset runAt) where T : notnull;

    /// <summary>Schedules an async job to run at a specific point in time.</summary>
    string ScheduleAt<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset runAt) where T : notnull;

    /// <summary>
    /// Adds or updates a recurring job using a Cron expression.
    /// If a job with the same <paramref name="recurringJobId"/> already exists, it is replaced.
    /// </summary>
    /// <typeparam name="T">The service type to resolve from DI when the job runs.</typeparam>
    /// <param name="recurringJobId">Stable identifier for this recurring job (used for updates/deletions).</param>
    /// <param name="methodCall">Expression referencing the method to call.</param>
    /// <param name="cronExpression">Cron expression (e.g., "0 * * * *" = hourly).</param>
    void AddOrUpdateRecurring<T>(
        string recurringJobId,
        Expression<Action<T>> methodCall,
        string cronExpression) where T : notnull;

    /// <summary>Adds or updates a recurring async job.</summary>
    void AddOrUpdateRecurring<T>(
        string recurringJobId,
        Expression<Func<T, Task>> methodCall,
        string cronExpression) where T : notnull;

    /// <summary>Removes a recurring job by its stable identifier. No-op if it does not exist.</summary>
    void RemoveRecurring(string recurringJobId);

    /// <summary>Triggers a recurring job immediately, regardless of its next scheduled time.</summary>
    void TriggerRecurring(string recurringJobId);

    /// <summary>Deletes a queued or scheduled job by its job ID. No-op if already executed.</summary>
    bool Delete(string jobId);
}
