namespace SharedCommon.ApiVersioning.UnitTests;

public sealed class ApiVersioningOptionsTests
{
    [Fact]
    public void DefaultVersion_Is_1_0()
    {
        var opts = new ApiVersioningOptions();
        Assert.Equal("1.0", opts.DefaultVersion);
    }

    [Fact]
    public void AssumeDefaultWhenUnspecified_DefaultsToTrue()
    {
        var opts = new ApiVersioningOptions();
        Assert.True(opts.AssumeDefaultWhenUnspecified);
    }

    [Fact]
    public void ReportApiVersions_DefaultsToTrue()
    {
        var opts = new ApiVersioningOptions();
        Assert.True(opts.ReportApiVersions);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("SharedCommon:ApiVersioning", ApiVersioningOptions.SectionName);
    }

    [Fact]
    public void Strategy_InitializedByDefault()
    {
        var opts = new ApiVersioningOptions();
        Assert.NotNull(opts.Strategy);
    }
}

public sealed class VersionReadingStrategyTests
{
    [Fact]
    public void UrlSegment_DefaultsToTrue()
    {
        var strategy = new VersionReadingStrategy();
        Assert.True(strategy.UrlSegment);
    }

    [Fact]
    public void QueryString_DefaultsToFalse()
    {
        var strategy = new VersionReadingStrategy();
        Assert.False(strategy.QueryString);
    }

    [Fact]
    public void QueryStringParameterName_DefaultsToApiVersion()
    {
        var strategy = new VersionReadingStrategy();
        Assert.Equal("api-version", strategy.QueryStringParameterName);
    }

    [Fact]
    public void Header_DefaultsToFalse()
    {
        var strategy = new VersionReadingStrategy();
        Assert.False(strategy.Header);
    }

    [Fact]
    public void HeaderName_DefaultsToXApiVersion()
    {
        var strategy = new VersionReadingStrategy();
        Assert.Equal("X-Api-Version", strategy.HeaderName);
    }

    [Fact]
    public void MediaType_DefaultsToFalse()
    {
        var strategy = new VersionReadingStrategy();
        Assert.False(strategy.MediaType);
    }
}
