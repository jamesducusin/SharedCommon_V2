namespace SharedCommon.HealthChecks.UnitTests;

public sealed class HealthCheckOptionsTests
{
    [Fact]
    public void DefaultTimeout_IsFiveSeconds() =>
        Assert.Equal(TimeSpan.FromSeconds(5), new HealthCheckOptions().DefaultTimeout);

    [Fact]
    public void Redis_IsNullByDefault() =>
        Assert.Null(new HealthCheckOptions().Redis);

    [Fact]
    public void ExternalHttp_IsEmptyByDefault() =>
        Assert.Empty(new HealthCheckOptions().ExternalHttp);

    [Fact]
    public void RedisCheckOptions_EnabledByDefault() =>
        Assert.True(new RedisCheckOptions().Enabled);

    [Fact]
    public void RedisCheckOptions_DefaultName_IsRedis() =>
        Assert.Equal("redis", new RedisCheckOptions().Name);

    [Fact]
    public void ExternalHttpCheckOptions_DefaultsAreEmpty()
    {
        var options = new ExternalHttpCheckOptions();
        Assert.Equal(string.Empty, options.Name);
        Assert.Equal(string.Empty, options.Uri);
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:HealthChecks", HealthCheckOptions.SectionName);
}
