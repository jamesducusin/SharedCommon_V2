namespace SharedCommon.Utilities.UnitTests;

public sealed class DateTimeExtensionsTests
{
    private static readonly DateTimeOffset Monday = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);   // Monday
    private static readonly DateTimeOffset Friday = new(2024, 1, 5, 12, 0, 0, TimeSpan.Zero);   // Friday
    private static readonly DateTimeOffset Saturday = new(2024, 1, 6, 12, 0, 0, TimeSpan.Zero); // Saturday

    [Fact]
    public void ToUtc_ConvertsToUniversalTime()
    {
        var local = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.FromHours(5));
        Assert.Equal(DateTimeKind.Utc, local.ToUtc().UtcDateTime.Kind);
    }

    [Fact]
    public void StartOfDay_ReturnsMidnight()
    {
        var result = Monday.StartOfDay();
        Assert.Equal(0, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
        Assert.Equal(0, result.Millisecond);
    }

    [Fact]
    public void EndOfDay_Returns23_59_59_999()
    {
        var result = Monday.EndOfDay();
        Assert.Equal(23, result.Hour);
        Assert.Equal(59, result.Minute);
        Assert.Equal(59, result.Second);
        Assert.Equal(999, result.Millisecond);
    }

    [Fact]
    public void IsBusinessDay_Weekday_ReturnsTrue() =>
        Assert.True(Monday.IsBusinessDay());

    [Fact]
    public void IsBusinessDay_Saturday_ReturnsFalse() =>
        Assert.False(Saturday.IsBusinessDay());

    [Fact]
    public void IsBusinessDay_Sunday_ReturnsFalse() =>
        Assert.False(new DateTimeOffset(2024, 1, 7, 0, 0, 0, TimeSpan.Zero).IsBusinessDay());

    [Fact]
    public void AddBusinessDays_SkipsWeekend_FromFriday()
    {
        var result = Friday.AddBusinessDays(1);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }

    [Fact]
    public void AddBusinessDays_Zero_ReturnsSameDate() =>
        Assert.Equal(Monday, Monday.AddBusinessDays(0));

    [Fact]
    public void AddBusinessDays_Negative_MovesBackward()
    {
        var result = Monday.AddBusinessDays(-1);
        Assert.Equal(DayOfWeek.Friday, result.DayOfWeek);
    }

    [Fact]
    public void ToUnixTimestamp_EpochReturnsZero()
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(0L, epoch.ToUnixTimestamp());
    }

    [Fact]
    public void ToUnixTimestampMilliseconds_EpochReturnsZero()
    {
        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(0L, epoch.ToUnixTimestampMilliseconds());
    }

    [Fact]
    public void IsInThePast_PastDate_ReturnsTrue() =>
        Assert.True(DateTimeOffset.UtcNow.AddDays(-1).IsInThePast());

    [Fact]
    public void IsInTheFuture_FutureDate_ReturnsTrue() =>
        Assert.True(DateTimeOffset.UtcNow.AddDays(1).IsInTheFuture());
}
