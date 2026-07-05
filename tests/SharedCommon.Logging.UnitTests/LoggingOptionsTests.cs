namespace SharedCommon.Logging.UnitTests;

public sealed class LoggingOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new LoggingOptions();
        Assert.Equal(string.Empty, options.ApplicationName);
        Assert.Equal("Information", options.MinimumLevel);
        Assert.True(options.AsyncMode);
        Assert.Empty(options.ExcludePatterns);
    }

    [Fact]
    public void ConsoleSinkOptions_DefaultsEnabled() =>
        Assert.True(new ConsoleSinkOptions().Enabled);

    [Fact]
    public void ConsoleSinkOptions_DefaultThemeColored() =>
        Assert.Equal("Colored", new ConsoleSinkOptions().Theme);

    [Fact]
    public void FileSinkOptions_DefaultsDisabled() =>
        Assert.False(new FileSinkOptions().Enabled);

    [Fact]
    public void FileSinkOptions_DefaultPath_IsRelative() =>
        Assert.StartsWith("./logs/", new FileSinkOptions().Path);

    [Fact]
    public void ElasticsearchSinkOptions_DefaultsDisabled() =>
        Assert.False(new ElasticsearchSinkOptions().Enabled);

    [Fact]
    public void DatabaseSinkOptions_DefaultsDisabled() =>
        Assert.False(new DatabaseSinkOptions().Enabled);

    [Fact]
    public void CorrelationIdOptions_DefaultHeaderName() =>
        Assert.Equal("X-Correlation-ID", new CorrelationIdOptions().HeaderName);

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Logging", LoggingOptions.SectionName);

    [Fact]
    public void DestructureOptions_DefaultMaxStringLength() =>
        Assert.Equal(4096, new DestructureOptions().MaxStringLength);
}
