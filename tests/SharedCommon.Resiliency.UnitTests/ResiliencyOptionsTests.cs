namespace SharedCommon.Resiliency.UnitTests;

public sealed class ResiliencyOptionsTests
{
    [Fact]
    public void RetryOptions_Defaults_AreCorrect()
    {
        var options = new RetryOptions();
        Assert.Equal(3, options.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), options.MaxDelay);
    }

    [Fact]
    public void CircuitBreakerOptions_Defaults_AreCorrect()
    {
        var options = new CircuitBreakerOptions();
        Assert.Equal(0.5, options.FailureRatio);
        Assert.Equal(5, options.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(60), options.SamplingDuration);
        Assert.Equal(TimeSpan.FromSeconds(30), options.BreakDuration);
    }

    [Fact]
    public void TimeoutOptions_Default_IsThirtySeconds() =>
        Assert.Equal(TimeSpan.FromSeconds(30), new TimeoutOptions().Duration);

    [Fact]
    public void ResiliencyOptions_ContainsAllPolicies()
    {
        var options = new ResiliencyOptions();
        Assert.NotNull(options.Retry);
        Assert.NotNull(options.CircuitBreaker);
        Assert.NotNull(options.Timeout);
    }

    [Fact]
    public void ResiliencyOptions_SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Resiliency", ResiliencyOptions.SectionName);
}
