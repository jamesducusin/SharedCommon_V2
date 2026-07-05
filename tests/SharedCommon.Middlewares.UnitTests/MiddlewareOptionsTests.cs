namespace SharedCommon.Middlewares.UnitTests;

public sealed class MiddlewareOptionsTests
{
    [Fact]
    public void ExceptionHandling_EnabledByDefault() =>
        Assert.True(new ExceptionHandlingOptions().Enabled);

    [Fact]
    public void ExceptionHandling_StackTrace_DisabledByDefault() =>
        Assert.False(new ExceptionHandlingOptions().IncludeStackTrace);

    [Fact]
    public void ExceptionHandling_LogsExceptionsByDefault() =>
        Assert.True(new ExceptionHandlingOptions().LogExceptions);

    [Fact]
    public void CorrelationIdMiddleware_EnabledByDefault() =>
        Assert.True(new CorrelationIdMiddlewareOptions().Enabled);

    [Fact]
    public void CorrelationIdMiddleware_DefaultHeader() =>
        Assert.Equal("X-Correlation-ID", new CorrelationIdMiddlewareOptions().HeaderName);

    [Fact]
    public void CorrelationIdMiddleware_GeneratesIfMissing() =>
        Assert.True(new CorrelationIdMiddlewareOptions().GenerateIfMissing);

    [Fact]
    public void RequestLogging_EnabledByDefault() =>
        Assert.True(new RequestLoggingOptions().Enabled);

    [Fact]
    public void RequestLogging_BodyLogging_DisabledByDefault()
    {
        var options = new RequestLoggingOptions();
        Assert.False(options.LogRequestBody);
        Assert.False(options.LogResponseBody);
    }

    [Fact]
    public void RequestLogging_ExcludesHealthAndMetricsByDefault()
    {
        var excluded = new RequestLoggingOptions().ExcludePaths;
        Assert.Contains("/health", excluded);
        Assert.Contains("/metrics", excluded);
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Middlewares", MiddlewareOptions.SectionName);
}
