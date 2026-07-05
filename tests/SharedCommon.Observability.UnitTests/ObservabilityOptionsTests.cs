namespace SharedCommon.Observability.UnitTests;

public sealed class ObservabilityOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new ObservabilityOptions();
        Assert.Equal(string.Empty, options.ServiceName);
        Assert.Equal("1.0.0", options.ServiceVersion);
        Assert.Null(options.OtlpEndpoint);
        Assert.Equal(1.0, options.SamplingRatio);
        Assert.True(options.InstrumentAspNetCore);
        Assert.True(options.InstrumentHttpClient);
    }

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Observability", ObservabilityOptions.SectionName);

    [Fact]
    public void SamplingRatio_DefaultIsAlwaysSample() =>
        Assert.Equal(1.0, new ObservabilityOptions().SamplingRatio);
}
