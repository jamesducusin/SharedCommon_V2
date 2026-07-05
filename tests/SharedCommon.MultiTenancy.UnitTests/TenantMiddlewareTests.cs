namespace SharedCommon.MultiTenancy.UnitTests;

/// <summary>
/// Behavioral tests for TenantMiddleware.
/// Verifies middleware correctly resolves tenants, populates context, and handles edge cases.
/// </summary>
public sealed class TenantMiddlewareTests
{
    private static IOptions<MultiTenancyOptions> Opts(MultiTenancyOptions opts) =>
        Microsoft.Extensions.Options.Options.Create(opts);

    private static HttpContext BuildContext() => new DefaultHttpContext();

    private static ILogger<TenantMiddleware> BuildLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ILogger<TenantMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_ResolvesAndSetsTenant_WhenResolverSucceeds()
    {
        // Arrange
        var tenantInfo = new TenantInfo("tenant-1", "Acme Corp");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantInfo));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        var nextCalled = false;
        RequestDelegate next = async ctx =>
        {
            Assert.True(tenantContext.IsResolved);
            Assert.Equal("tenant-1", tenantContext.TenantId);
            nextCalled = true;
            await Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.True(nextCalled);
        Assert.True(tenantContext.IsResolved);
        Assert.Equal("tenant-1", tenantContext.TenantId);
        Assert.Equal("Acme Corp", tenantContext.TenantName);
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughWithoutResolvingWhenDisabled()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        var nextCalled = false;
        RequestDelegate next = async _ => { nextCalled = true; await Task.CompletedTask; };

        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = false }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.True(nextCalled);
        Assert.False(tenantContext.IsResolved);
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughWhenNoTenantAndNotRequired()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        var nextCalled = false;
        RequestDelegate next = async _ => { nextCalled = true; await Task.CompletedTask; };

        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true, RequireTenant = false }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.True(nextCalled);
        Assert.False(tenantContext.IsResolved);
    }

    [Fact]
    public async Task InvokeAsync_Returns400_WhenNoTenantAndRequired()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        var nextCalled = false;
        RequestDelegate next = async _ => { nextCalled = true; await Task.CompletedTask; };

        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true, RequireTenant = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_PreservesPropertiesInTenantContext()
    {
        // Arrange
        var properties = new Dictionary<string, string> { ["Plan"] = "Pro", ["Features"] = "Premium" };
        var tenantInfo = new TenantInfo("t-123", "Tenant123", properties);
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantInfo));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.True(tenantContext.IsResolved);
        Assert.Equal("Pro", tenantContext.Properties["Plan"]);
        Assert.Equal("Premium", tenantContext.Properties["Features"]);
    }

    [Fact]
    public async Task InvokeAsync_AllowsNextMiddlewareToAccessTenantContext()
    {
        // Arrange
        var tenantInfo = new TenantInfo("downstream-tenant", "Downstream Corp");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantInfo));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        var tenantSeenByNext = string.Empty;
        RequestDelegate next = async ctx =>
        {
            tenantSeenByNext = tenantContext.TenantId;
            await Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.Equal("downstream-tenant", tenantSeenByNext);
    }

    [Fact]
    public async Task InvokeAsync_ResolveAsync_UsesDefaultCancellationToken()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true, RequireTenant = false }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        await resolver.Received(1).ResolveAsync(context, default);
    }

    [Fact]
    public async Task InvokeAsync_PassesCorrectHttpContextToResolver()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var context = BuildContext();
        context.Request.Headers["X-Request-Id"] = "req-123";

        var tenantContext = new TenantContext();
        var logger = BuildLogger();
        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        await resolver.Received(1).ResolveAsync(context, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_DoesNotModifyResponseWhenTenantResolved()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(new TenantInfo("t-ok")));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HandlesNullTenantName()
    {
        // Arrange
        var tenantInfo = new TenantInfo("t-no-name");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantInfo));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert
        Assert.True(tenantContext.IsResolved);
        Assert.Equal("t-no-name", tenantContext.TenantId);
        Assert.Null(tenantContext.TenantName);
    }

    [Fact]
    public async Task InvokeAsync_ContextInitiallyUnresolved()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var context = BuildContext();
        var tenantContext = new TenantContext();
        var logger = BuildLogger();

        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true, RequireTenant = false }), logger);

        // Act & Assert
        Assert.Empty(tenantContext.TenantId);
        Assert.False(tenantContext.IsResolved);

        await middleware.InvokeAsync(context, resolver, tenantContext);

        Assert.Empty(tenantContext.TenantId);
        Assert.False(tenantContext.IsResolved);
    }
}
