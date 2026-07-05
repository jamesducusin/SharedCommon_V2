namespace SharedCommon.FeatureFlags.UnitTests;

public sealed class FeatureFlagOptionsTests
{
    [Fact]
    public void CacheTtlSeconds_DefaultsToZero()
    {
        var opts = new FeatureFlagOptions();
        Assert.Equal(0, opts.CacheTtlSeconds);
    }

    [Fact]
    public void LogEvaluations_DefaultsToTrue()
    {
        var opts = new FeatureFlagOptions();
        Assert.True(opts.LogEvaluations);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("SharedCommon:FeatureFlags", FeatureFlagOptions.SectionName);
    }
}

public sealed class FeatureFlagContextTests
{
    [Fact]
    public void DefaultConstruction_AllNullable()
    {
        var ctx = new FeatureFlagContext();
        Assert.Null(ctx.UserId);
        Assert.Null(ctx.TenantId);
        Assert.Null(ctx.Groups);
    }

    [Fact]
    public void CanSet_UserId()
    {
        var ctx = new FeatureFlagContext(UserId: "u-123");
        Assert.Equal("u-123", ctx.UserId);
    }

    [Fact]
    public void CanSet_TenantId()
    {
        var ctx = new FeatureFlagContext(TenantId: "tenant-42");
        Assert.Equal("tenant-42", ctx.TenantId);
    }

    [Fact]
    public void CanSet_Groups()
    {
        var groups = new[] { "beta", "internal" };
        var ctx = new FeatureFlagContext(Groups: groups);
        Assert.Equal(groups, ctx.Groups);
    }

    [Fact]
    public void Record_Equality_WorksByValue()
    {
        var a = new FeatureFlagContext("u1", "t1", null);
        var b = new FeatureFlagContext("u1", "t1", null);
        Assert.Equal(a, b);
    }
}
