using System.Linq.Expressions;
using Hangfire;

namespace SharedCommon.BackgroundJobs;

/// <summary>
/// <see cref="IBackgroundJobService"/> implementation backed by Hangfire.
/// All methods delegate directly to Hangfire's <see cref="IBackgroundJobClient"/>
/// and <see cref="IRecurringJobManager"/>.
/// </summary>
internal sealed class HangfireBackgroundJobService(
    IBackgroundJobClient client,
    IRecurringJobManager recurring) : IBackgroundJobService
{
    public string Enqueue<T>(Expression<Action<T>> methodCall) where T : notnull
        => client.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) where T : notnull
        => client.Enqueue(methodCall);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) where T : notnull
        => client.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) where T : notnull
        => client.Schedule(methodCall, delay);

    public string ScheduleAt<T>(Expression<Action<T>> methodCall, DateTimeOffset runAt) where T : notnull
        => client.Schedule(methodCall, runAt);

    public string ScheduleAt<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset runAt) where T : notnull
        => client.Schedule(methodCall, runAt);

    public void AddOrUpdateRecurring<T>(
        string recurringJobId,
        Expression<Action<T>> methodCall,
        string cronExpression) where T : notnull
        => recurring.AddOrUpdate(recurringJobId, methodCall, cronExpression);

    public void AddOrUpdateRecurring<T>(
        string recurringJobId,
        Expression<Func<T, Task>> methodCall,
        string cronExpression) where T : notnull
        => recurring.AddOrUpdate(recurringJobId, methodCall, cronExpression);

    public void RemoveRecurring(string recurringJobId)
        => recurring.RemoveIfExists(recurringJobId);

    public void TriggerRecurring(string recurringJobId)
        => recurring.Trigger(recurringJobId);

    public bool Delete(string jobId)
        => client.Delete(jobId);
}
