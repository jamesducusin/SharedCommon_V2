namespace SharedCommon.Caching.UnitTests;

public sealed class CachingOptionsTests
{
    [Fact]
    public void CachingOptions_Defaults_AreCorrect()
    {
        var options = new CachingOptions();
        Assert.Equal("Hybrid", options.DefaultProvider);
        Assert.Equal(300, options.DefaultTtlSeconds);
        Assert.Equal("Json", options.SerializationFormat);
    }

    [Fact]
    public void MemoryCacheOptions_DefaultsEnabled() =>
        Assert.True(new MemoryCacheOptions().Enabled);

    [Fact]
    public void MemoryCacheOptions_DefaultMaxSize() =>
        Assert.Equal(10_000, new MemoryCacheOptions().MaximumSize);

    [Fact]
    public void RedisCacheOptions_DefaultsDisabled() =>
        Assert.False(new RedisCacheOptions().Enabled);

    [Fact]
    public void RedisCacheOptions_DefaultKeyPrefix() =>
        Assert.Equal("sharedcommon:", new RedisCacheOptions().KeyPrefix);

    [Fact]
    public void HybridCacheOptions_L1EnabledByDefault() =>
        Assert.True(new HybridCacheOptions().L1Enabled);

    [Fact]
    public void HybridCacheOptions_L2DisabledByDefault() =>
        Assert.False(new HybridCacheOptions().L2Enabled);

    [Fact]
    public void HybridCacheOptions_PromoteOnHitEnabled() =>
        Assert.True(new HybridCacheOptions().PromoteOnHit);

    [Fact]
    public void CacheKeyPolicyOptions_DefaultSeparator() =>
        Assert.Equal(":", new CacheKeyPolicyOptions().Separator);

    [Fact]
    public void SectionName_IsCorrect() =>
        Assert.Equal("SharedCommon:Caching", CachingOptions.SectionName);
}
