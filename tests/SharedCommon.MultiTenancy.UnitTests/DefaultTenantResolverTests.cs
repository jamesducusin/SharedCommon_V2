using System.Security.Claims;

namespace SharedCommon.MultiTenancy.UnitTests;

public sealed class DefaultTenantResolverTests
{
    private static IOptions<MultiTenancyOptions> Opts(MultiTenancyOptions opts) =>
        Microsoft.Extensions.Options.Options.Create(opts);

    private static HttpContext BuildContext(Action<HttpContext> configure)
    {
        var ctx = new DefaultHttpContext();
        configure(ctx);
        return ctx;
    }

    [Fact]
    public async Task Header_Strategy_ResolvesFromHeader()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Header, HeaderName = "X-Tenant-Id" });
        var ctx = BuildContext(c => c.Request.Headers["X-Tenant-Id"] = "tenant-99");

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.NotNull(result);
        Assert.Equal("tenant-99", result!.TenantId);
    }

    [Fact]
    public async Task Header_Strategy_ReturnsNull_WhenHeaderMissing()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Header });
        var ctx = BuildContext(_ => { });

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task Claim_Strategy_ResolvesFromJwtClaim()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Claim, ClaimName = "tenant_id" });
        var ctx = BuildContext(c =>
        {
            var identity = new ClaimsIdentity(new[] { new Claim("tenant_id", "tenant-from-claim") }, "test");
            c.User = new ClaimsPrincipal(identity);
        });

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.NotNull(result);
        Assert.Equal("tenant-from-claim", result!.TenantId);
    }

    [Fact]
    public async Task Claim_Strategy_ReturnsNull_WhenClaimMissing()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Claim, ClaimName = "tenant_id" });
        var ctx = BuildContext(_ => { });

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task Subdomain_Strategy_ResolvesFirstHostLabel()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Subdomain });
        var ctx = BuildContext(c => c.Request.Host = new HostString("acme.api.example.com"));

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.NotNull(result);
        Assert.Equal("acme", result!.TenantId);
    }

    [Fact]
    public async Task Subdomain_Strategy_ReturnsNull_ForApexDomain()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Subdomain });
        var ctx = BuildContext(c => c.Request.Host = new HostString("example.com"));

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryString_Strategy_ResolvesFromQueryParam()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.QueryString, QueryStringKey = "tenantId" });
        var ctx = BuildContext(c => c.Request.QueryString = new QueryString("?tenantId=qs-tenant"));

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.NotNull(result);
        Assert.Equal("qs-tenant", result!.TenantId);
    }

    [Fact]
    public async Task QueryString_Strategy_ReturnsNull_WhenParamMissing()
    {
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.QueryString, QueryStringKey = "tenantId" });
        var ctx = BuildContext(_ => { });

        var resolver = new DefaultTenantResolver(opts);
        var result = await resolver.ResolveAsync(ctx);

        Assert.Null(result);
    }
}
