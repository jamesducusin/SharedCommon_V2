namespace SharedCommon.Utilities;

/// <summary>Extension methods for <see cref="DateTimeOffset"/> and <see cref="DateTime"/>.</summary>
public static class DateTimeExtensions
{
    /// <summary>Returns the value expressed in UTC.</summary>
    public static DateTimeOffset ToUtc(this DateTimeOffset value) => value.ToUniversalTime();

    /// <summary>Returns midnight (00:00:00.000) on the same date, preserving the offset.</summary>
    public static DateTimeOffset StartOfDay(this DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, 0, value.Offset);

    /// <summary>Returns 23:59:59.999 on the same date, preserving the offset.</summary>
    public static DateTimeOffset EndOfDay(this DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, 23, 59, 59, 999, value.Offset);

    /// <summary>
    /// Adds <paramref name="days"/> business days (Mon–Fri), skipping weekends.
    /// Negative values move backward.
    /// </summary>
    public static DateTimeOffset AddBusinessDays(this DateTimeOffset value, int days)
    {
        if (days == 0) return value;

        var direction = days > 0 ? 1 : -1;
        var remaining = Math.Abs(days);
        var current = value;

        while (remaining > 0)
        {
            current = current.AddDays(direction);
            if (current.IsBusinessDay())
                remaining--;
        }

        return current;
    }

    /// <summary>Returns <c>true</c> if the date falls on a weekday (Mon–Fri).</summary>
    public static bool IsBusinessDay(this DateTimeOffset value) =>
        value.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    /// <summary>Converts to Unix time in seconds (seconds since 1970-01-01T00:00:00Z).</summary>
    public static long ToUnixTimestamp(this DateTimeOffset value) =>
        value.ToUniversalTime().ToUnixTimeSeconds();

    /// <summary>Converts to Unix time in milliseconds.</summary>
    public static long ToUnixTimestampMilliseconds(this DateTimeOffset value) =>
        value.ToUniversalTime().ToUnixTimeMilliseconds();

    /// <summary>Returns <c>true</c> if the value is in the past relative to <see cref="DateTimeOffset.UtcNow"/>.</summary>
    public static bool IsInThePast(this DateTimeOffset value) => value < DateTimeOffset.UtcNow;

    /// <summary>Returns <c>true</c> if the value is in the future relative to <see cref="DateTimeOffset.UtcNow"/>.</summary>
    public static bool IsInTheFuture(this DateTimeOffset value) => value > DateTimeOffset.UtcNow;
}
