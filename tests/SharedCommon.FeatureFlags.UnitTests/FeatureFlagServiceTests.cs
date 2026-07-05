namespace SharedCommon.FeatureFlags.UnitTests;

public sealed class FeatureFlagServiceTests
{
    private static IFeatureFlagService BuildService(
        Dictionary<string, string?> flags)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(flags)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedFeatureFlags(config);

        return services.BuildServiceProvider()
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IFeatureFlagService>();
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsTrue_WhenFlagIsOn()
    {
        var svc = BuildService(new() { ["FeatureManagement:NewFlow"] = "true" });
        Assert.True(await svc.IsEnabledAsync("NewFlow"));
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsFalse_WhenFlagIsOff()
    {
        var svc = BuildService(new() { ["FeatureManagement:OldFlow"] = "false" });
        Assert.False(await svc.IsEnabledAsync("OldFlow"));
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsFalse_ForUndefinedFlag()
    {
        var svc = BuildService(new());
        Assert.False(await svc.IsEnabledAsync("UndefinedFlag"));
    }

    [Fact]
    public async Task GetEnabledFeaturesAsync_ReturnsOnlyEnabledFlags()
    {
        var svc = BuildService(new()
        {
            ["FeatureManagement:Alpha"] = "true",
            ["FeatureManagement:Beta"] = "false",
            ["FeatureManagement:Gamma"] = "true",
        });

        var enabled = await svc.GetEnabledFeaturesAsync();

        Assert.Contains("Alpha", enabled);
        Assert.Contains("Gamma", enabled);
        Assert.DoesNotContain("Beta", enabled);
    }

    [Fact]
    public async Task GetEnabledFeaturesAsync_ReturnsEmpty_WhenNoneEnabled()
    {
        var svc = BuildService(new()
        {
            ["FeatureManagement:X"] = "false",
            ["FeatureManagement:Y"] = "false",
        });

        var enabled = await svc.GetEnabledFeaturesAsync();
        Assert.Empty(enabled);
    }
}
