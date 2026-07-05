namespace SharedCommon.BackgroundJobs.UnitTests;

public sealed class CronTests
{
    [Theory]
    [InlineData(Cron.EveryMinute, "* * * * *")]
    [InlineData(Cron.Every5Minutes, "*/5 * * * *")]
    [InlineData(Cron.Every15Minutes, "*/15 * * * *")]
    [InlineData(Cron.Every30Minutes, "*/30 * * * *")]
    [InlineData(Cron.Hourly, "0 * * * *")]
    [InlineData(Cron.Daily, "0 0 * * *")]
    [InlineData(Cron.DailyOffPeak, "0 3 * * *")]
    [InlineData(Cron.Weekly, "0 0 * * 1")]
    [InlineData(Cron.Monthly, "0 0 1 * *")]
    public void CronConstant_HasCorrectExpression(string actual, string expected) =>
        Assert.Equal(expected, actual);

    [Fact]
    public void AllCronExpressions_HaveFiveParts()
    {
        var expressions = new[]
        {
            Cron.EveryMinute, Cron.Every5Minutes, Cron.Every15Minutes,
            Cron.Every30Minutes, Cron.Hourly, Cron.Daily, Cron.DailyOffPeak,
            Cron.Weekly, Cron.Monthly
        };

        foreach (var expr in expressions)
            Assert.Equal(5, expr.Split(' ').Length);
    }
}
