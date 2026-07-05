namespace SharedCommon.BackgroundJobs;

/// <summary>
/// Named Cron expression constants for common recurring job schedules.
///
/// <code>
/// jobs.AddOrUpdateRecurring&lt;ICleanupService&gt;(
///     "cleanup-expired-sessions",
///     svc => svc.CleanExpiredAsync(),
///     Cron.Daily);
/// </code>
/// </summary>
public static class Cron
{
    /// <summary>Every minute: <c>* * * * *</c></summary>
    public const string EveryMinute = "* * * * *";

    /// <summary>Every 5 minutes: <c>*/5 * * * *</c></summary>
    public const string Every5Minutes = "*/5 * * * *";

    /// <summary>Every 15 minutes: <c>*/15 * * * *</c></summary>
    public const string Every15Minutes = "*/15 * * * *";

    /// <summary>Every 30 minutes: <c>*/30 * * * *</c></summary>
    public const string Every30Minutes = "*/30 * * * *";

    /// <summary>Every hour at :00: <c>0 * * * *</c></summary>
    public const string Hourly = "0 * * * *";

    /// <summary>Every day at midnight UTC: <c>0 0 * * *</c></summary>
    public const string Daily = "0 0 * * *";

    /// <summary>Every day at 3 AM UTC (off-peak): <c>0 3 * * *</c></summary>
    public const string DailyOffPeak = "0 3 * * *";

    /// <summary>Every Monday at midnight UTC: <c>0 0 * * 1</c></summary>
    public const string Weekly = "0 0 * * 1";

    /// <summary>First day of every month at midnight UTC: <c>0 0 1 * *</c></summary>
    public const string Monthly = "0 0 1 * *";
}
