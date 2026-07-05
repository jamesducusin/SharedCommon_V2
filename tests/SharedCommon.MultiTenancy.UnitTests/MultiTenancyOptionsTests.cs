namespace SharedCommon.MultiTenancy.UnitTests;

public sealed class MultiTenancyOptionsTests
{
    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("SharedCommon:MultiTenancy", MultiTenancyOptions.SectionName);
    }

    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        var opts = new MultiTenancyOptions();
        Assert.True(opts.Enabled);
    }

    [Fact]
    public void Strategy_DefaultsToHeader()
    {
        var opts = new MultiTenancyOptions();
        Assert.Equal(TenantResolutionStrategy.Header, opts.Strategy);
    }

    [Fact]
    public void HeaderName_DefaultsToXTenantId()
    {
        var opts = new MultiTenancyOptions();
        Assert.Equal("X-Tenant-Id", opts.HeaderName);
    }

    [Fact]
    public void ClaimName_DefaultsToTenantId()
    {
        var opts = new MultiTenancyOptions();
        Assert.Equal("tenant_id", opts.ClaimName);
    }

    [Fact]
    public void QueryStringKey_DefaultsToTenantId()
    {
        var opts = new MultiTenancyOptions();
        Assert.Equal("tenantId", opts.QueryStringKey);
    }

    [Fact]
    public void RequireTenant_DefaultsToFalse()
    {
        var opts = new MultiTenancyOptions();
        Assert.False(opts.RequireTenant);
    }

    [Fact]
    public void TenantResolutionStrategy_EnumHasAllStrategies()
    {
        var values = Enum.GetValues<TenantResolutionStrategy>();
        Assert.Contains(TenantResolutionStrategy.Header, values);
        Assert.Contains(TenantResolutionStrategy.Claim, values);
        Assert.Contains(TenantResolutionStrategy.Subdomain, values);
        Assert.Contains(TenantResolutionStrategy.QueryString, values);
    }
}

public sealed class TenantInfoTests
{
    [Fact]
    public void Constructor_SetsRequiredProperties()
    {
        var info = new TenantInfo("tenant-abc");
        Assert.Equal("tenant-abc", info.TenantId);
        Assert.Null(info.TenantName);
        Assert.Null(info.Properties);
    }

    [Fact]
    public void Constructor_SetsOptionalProperties()
    {
        var props = new Dictionary<string, string> { ["Plan"] = "Pro" };
        var info = new TenantInfo("t1", "Acme Corp", props);
        Assert.Equal("t1", info.TenantId);
        Assert.Equal("Acme Corp", info.TenantName);
        Assert.Equal("Pro", info.Properties!["Plan"]);
    }

    [Fact]
    public void Record_Equality_WorksByValue()
    {
        var a = new TenantInfo("t1", "Acme");
        var b = new TenantInfo("t1", "Acme");
        Assert.Equal(a, b);
    }
}
