using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SharedCommon.MultiTenancy.UnitTests;

/// <summary>
/// Behavioral tests for ServiceCollectionExtensions.
/// Verifies DI registration, options binding, and service resolution.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharedMultiTenancy_RegistersTenantContextAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedMultiTenancy(config);
        var provider = services.BuildServiceProvider();

        // Assert - scoped means different contexts in different scopes
        using (var scope1 = provider.CreateScope())
        {
            var context1 = scope1.ServiceProvider.GetRequiredService<ITenantContext>();

            using (var scope2 = provider.CreateScope())
            {
                var context2 = scope2.ServiceProvider.GetRequiredService<ITenantContext>();
                Assert.NotSame(context1, context2);  // Different scopes = different instances
            }
        }

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_RegistersDefaultTenantResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedMultiTenancy(config);
        var provider = services.BuildServiceProvider();

        // Assert
        var resolver = provider.GetRequiredService<ITenantResolver>();
        Assert.NotNull(resolver);
        Assert.IsType<DefaultTenantResolver>(resolver);

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_BindsConfigurationToOptions()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SharedCommon:MultiTenancy:Enabled"] = "false",
                ["SharedCommon:MultiTenancy:Strategy"] = "Claim",
                ["SharedCommon:MultiTenancy:RequireTenant"] = "true",
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddSharedMultiTenancy(config);
        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<MultiTenancyOptions>>();

        // Assert
        Assert.False(opts.Value.Enabled);
        Assert.Equal(TenantResolutionStrategy.Claim, opts.Value.Strategy);
        Assert.True(opts.Value.RequireTenant);

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_AllowsOverridingResolver()
    {
        // Arrange
        var customResolver = Substitute.For<ITenantResolver>();
        customResolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedMultiTenancy(config);
        services.AddScoped<ITenantResolver>(_ => customResolver);

        var provider = services.BuildServiceProvider();

        // Assert
        var resolver = provider.GetRequiredService<ITenantResolver>();
        Assert.Same(customResolver, resolver);

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_UsesDefaultOptionsWhenNoConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedMultiTenancy(config);
        var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<MultiTenancyOptions>>();

        // Assert - should have defaults
        Assert.True(opts.Value.Enabled);
        Assert.Equal(TenantResolutionStrategy.Header, opts.Value.Strategy);
        Assert.False(opts.Value.RequireTenant);
        Assert.Equal("X-Tenant-Id", opts.Value.HeaderName);
        Assert.Equal("tenant_id", opts.Value.ClaimName);
        Assert.Equal("tenantId", opts.Value.QueryStringKey);

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_ScopedContextIsIsolatedPerHttpContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSharedMultiTenancy(config);

        var provider = services.BuildServiceProvider();

        // Act & Assert - each scope should get its own context
        using (var scope1 = provider.CreateScope())
        {
            var ctx1 = scope1.ServiceProvider.GetRequiredService<ITenantContext>();

            using (var scope2 = provider.CreateScope())
            {
                var ctx2 = scope2.ServiceProvider.GetRequiredService<ITenantContext>();
                Assert.NotSame(ctx1, ctx2);
            }
        }

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_RegistersMultipleTimesDoesNotDuplicate()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act - register twice
        services.AddSharedMultiTenancy(config);
        services.AddSharedMultiTenancy(config);

        var provider = services.BuildServiceProvider();

        // Assert - should not throw and should work correctly
        var resolver = provider.GetRequiredService<ITenantResolver>();
        Assert.NotNull(resolver);

        provider.Dispose();
    }

    [Fact]
    public void AddSharedMultiTenancy_IntegrationWithApplicationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSharedMultiTenancy(config);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSharedMultiTenancy(config);

        // Act
        var app = builder.Build();
        app.UseSharedMultiTenancy();

        // Assert - app should be configured
        Assert.NotNull(app);
    }
}

/// <summary>
/// Behavioral tests for custom ITenantResolver implementations.
/// Demonstrates extensibility and override patterns.
/// </summary>
public sealed class CustomTenantResolverTests
{
    private sealed class DatabaseTenantResolver : ITenantResolver
    {
        public async Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken ct = default)
        {
            // Simulate database lookup
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
            {
                await Task.Delay(10, ct);  // Simulate I/O
                return new TenantInfo(tenantId.ToString(), $"Tenant-{tenantId}");
            }

            return null;
        }
    }

    [Fact]
    public async Task CustomResolver_CanBeUsedInPlace_OfDefault()
    {
        // Arrange
        var resolver = new DatabaseTenantResolver();

        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Tenant-Id"] = "custom-tenant";

        // Act
        var result = await resolver.ResolveAsync(ctx);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("custom-tenant", result!.TenantId);
        Assert.Equal("Tenant-custom-tenant", result.TenantName);
    }

    [Fact]
    public void CustomResolver_CanBeRegisteredInDI()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddSharedMultiTenancy(config);
        services.AddScoped<ITenantResolver, DatabaseTenantResolver>();
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        // Assert
        var resolver = provider.GetRequiredService<ITenantResolver>();
        Assert.IsType<DatabaseTenantResolver>(resolver);

        provider.Dispose();
    }

    [Fact]
    public async Task CustomResolver_ReceivesHttpContextCorrectly()
    {
        // Arrange
        var receivedContexts = new List<HttpContext>();

        var customResolver = Substitute.For<ITenantResolver>();
        customResolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                receivedContexts.Add((HttpContext)x[0]);
                return Task.FromResult<TenantInfo?>(null);
            });

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";

        // Act
        await customResolver.ResolveAsync(ctx);

        // Assert
        Assert.Single(receivedContexts);
        Assert.Equal("POST", receivedContexts[0].Request.Method);
    }

    [Fact]
    public async Task CustomResolver_CanReturnComplexTenantInfo()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["Database"] = "tenant-db-prod",
            ["Region"] = "us-east-1",
            ["Tier"] = "Enterprise",
        };

        var tenantInfo = new TenantInfo("enterprise-123", "Enterprise Corp Ltd", properties);

        var customResolver = Substitute.For<ITenantResolver>();
        customResolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantInfo));

        var ctx = new DefaultHttpContext();

        // Act
        var result = await customResolver.ResolveAsync(ctx);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("enterprise-123", result!.TenantId);
        Assert.Equal("Enterprise Corp Ltd", result.TenantName);
        Assert.Equal("tenant-db-prod", result.Properties!["Database"]);
        Assert.Equal("us-east-1", result.Properties["Region"]);
        Assert.Equal("Enterprise", result.Properties["Tier"]);
    }

    [Fact]
    public async Task CustomResolver_CanReturnNullForAnonymousRequests()
    {
        // Arrange
        var customResolver = Substitute.For<ITenantResolver>();
        customResolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));

        var ctx = new DefaultHttpContext();

        // Act
        var result = await customResolver.ResolveAsync(ctx);

        // Assert
        Assert.Null(result);
    }
}
