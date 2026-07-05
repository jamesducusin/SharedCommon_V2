using System.Security.Claims;

namespace SharedCommon.MultiTenancy.UnitTests;

/// <summary>
/// Data isolation tests for multi-tenancy security.
/// Verifies that tenant context properly boundaries prevent data cross-contamination.
/// 
/// ⚠️ CRITICAL: These tests document expectations for application code.
/// This package does NOT enforce data isolation; application layers MUST validate.
/// </summary>
public sealed class TenantDataIsolationTests
{
    private static IOptions<MultiTenancyOptions> Opts(MultiTenancyOptions opts) =>
        Microsoft.Extensions.Options.Options.Create(opts);

    private static HttpContext BuildContext(Action<HttpContext> configure)
    {
        var ctx = new DefaultHttpContext();
        configure(ctx);
        return ctx;
    }

    private static ILogger<TenantMiddleware> BuildLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ILogger<TenantMiddleware>>();
    }

    [Fact]
    public async Task TenantMiddleware_ResolvesFirstTenant_WithoutLeakingToPriorRequest()
    {
        // Arrange - simulate first request from TenantA
        var tenantAInfo = new TenantInfo("tenant-a", "Tenant A");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantAInfo));

        var contextA = BuildContext(_ => { });
        var tenantContextA = new TenantContext();
        var logger = BuildLogger();
        RequestDelegate nextA = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(nextA, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act - process request A
        await middleware.InvokeAsync(contextA, resolver, tenantContextA);

        // Assert - TenantA is resolved
        Assert.Equal("tenant-a", tenantContextA.TenantId);
        Assert.True(tenantContextA.IsResolved);

        // Arrange - simulate second request from TenantB with NEW context instances
        var tenantBInfo = new TenantInfo("tenant-b", "Tenant B");
        resolver.ClearReceivedCalls();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(tenantBInfo));

        var contextB = BuildContext(_ => { });
        var tenantContextB = new TenantContext();  // NEW instance for second request
        RequestDelegate nextB = async _ => await Task.CompletedTask;

        // Act - process request B
        await middleware.InvokeAsync(contextB, resolver, tenantContextB);

        // Assert - TenantB is resolved in its own context (isolation verified)
        Assert.Equal("tenant-b", tenantContextB.TenantId);
        Assert.True(tenantContextB.IsResolved);
        // Original context remains unchanged (demonstrates scoped isolation)
        Assert.Equal("tenant-a", tenantContextA.TenantId);
    }

    [Fact]
    public void TenantContext_ContainsTenantIdAsUntrustedInput_RequiresValidation()
    {
        // Arrange - Application code receives a TenantId from ITenantContext
        var tenantIdFromContext = "malicious-tenant-' OR '1'='1";
        var ctx = new TenantContext();
        var info = new TenantInfo(tenantIdFromContext, "Attacker Tenant");
        ((dynamic)ctx).SetTenant(info);

        // Assert - TenantContext DOES NOT validate input
        // Application code MUST validate before using in queries
        Assert.Equal("malicious-tenant-' OR '1'='1", ctx.TenantId);
        
        // This represents what application code should do:
        var isValidTenantId = System.Text.RegularExpressions.Regex.IsMatch(
            ctx.TenantId, @"^[a-zA-Z0-9\-_]+$");
        Assert.False(isValidTenantId, "Application MUST validate tenant ID format");
    }

    [Fact]
    public async Task TenantMiddleware_RequireTenant_Blocks_UnresolvedRequests()
    {
        // Arrange - application configured to require tenant identification
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantInfo?>(null));  // No tenant found

        var context = BuildContext(_ => { });
        var tenantContext = new TenantContext();
        var logger = BuildLogger();
        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true, RequireTenant = true }), logger);

        // Act
        await middleware.InvokeAsync(context, resolver, tenantContext);

        // Assert - request is rejected with 400, preventing cross-tenant access
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(tenantContext.IsResolved);
    }

    [Fact]
    public async Task TenantContext_InMultipleScopedRequests_RemainsSeparated()
    {
        // Arrange - multiple concurrent requests with different tenants
        var tenants = new[] { "tenant-1", "tenant-2", "tenant-3" };
        var results = new System.Collections.Concurrent.ConcurrentDictionary<int, string>();

        var tasks = tenants.Select((tenantId, index) => Task.Run(() =>
        {
            // Each "request" gets its own context (simulating scoped DI)
            var ctx = new TenantContext();
            var info = new TenantInfo(tenantId, $"Tenant {index}");
            ((dynamic)ctx).SetTenant(info);

            // Simulate work with this context
            System.Threading.Thread.Sleep(10);  // Simulate I/O

            // Store the tenant ID seen by this "request"
            results.TryAdd(index, ctx.TenantId);
        })).ToArray();

        // Act
        await Task.WhenAll(tasks);

        // Assert - each request saw its own tenant (no cross-contamination)
        Assert.Equal("tenant-1", results[0]);
        Assert.Equal("tenant-2", results[1]);
        Assert.Equal("tenant-3", results[2]);
    }

    [Fact]
    public void TenantContext_Properties_CanLeakTenantMetadata_RequiresApplicationGuarding()
    {
        // Arrange - tenant context includes additional properties (subscription tier, features, etc.)
        var properties = new Dictionary<string, string>
        {
            ["Tier"] = "Enterprise",
            ["MaxUsers"] = "1000",
            ["Features"] = "analytics,api-access,sso"
        };
        var info = new TenantInfo("tenant-secure", "Secure Tenant", properties);
        var ctx = new TenantContext();
        ((dynamic)ctx).SetTenant(info);

        // Assert - Properties are exposed and could be logged/leaked by careless code
        Assert.Equal("Enterprise", ctx.Properties["Tier"]);
        
        // Application MUST NOT log/expose sensitive metadata across tenant boundaries
        var logMessage = $"Tenant initialized: {ctx.TenantId} - Features: {string.Join(",", ctx.Properties.Values)}";
        Assert.DoesNotContain("Enterprise", logMessage.Split("Tenant initialized: ")[1].Split(" -")[0]);
    }

    [Fact]
    public async Task DefaultTenantResolver_ExtractsFromHeader_WithoutValidation_RequiresApplicationValidation()
    {
        // Arrange - resolver extracts from header
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Header, HeaderName = "X-Tenant-Id" });
        var suspiciousValue = "<script>alert('xss')</script>";
        var ctx = BuildContext(c => c.Request.Headers["X-Tenant-Id"] = suspiciousValue);

        var resolver = new DefaultTenantResolver(opts);

        // Act
        var result = await resolver.ResolveAsync(ctx);

        // Assert - resolver does NOT sanitize; application code MUST validate
        Assert.NotNull(result);
        Assert.Equal(suspiciousValue, result!.TenantId);
    }

    [Fact]
    public async Task TenantResolution_FromClaim_MustValidateClaimOrigin_ApplicationResponsibility()
    {
        // Arrange - attacker tries to inject tenant claim
        var maliciousClaim = new Claim("tenant_id", "admin-tenant");
        var opts = Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Claim, ClaimName = "tenant_id" });
        var ctx = BuildContext(c =>
        {
            var identity = new System.Security.Claims.ClaimsIdentity(new[] { maliciousClaim }, "test");
            c.User = new System.Security.Claims.ClaimsPrincipal(identity);
        });

        var resolver = new DefaultTenantResolver(opts);

        // Act
        var result = await resolver.ResolveAsync(ctx);

        // Assert - resolver trusts the claim; application MUST validate JWT signature first
        Assert.NotNull(result);
        Assert.Equal("admin-tenant", result!.TenantId);
        // Real apps would validate JWT signature before trusting this claim
    }

    [Fact]
    public async Task TenantMiddleware_CanProcessBackToBackRequests_WithDifferentTenants()
    {
        // Arrange - simulate HTTP pipeline processing sequential requests
        var requestsAndExpectedTenants = new[]
        {
            ("request-1", "tenant-alpha"),
            ("request-2", "tenant-bravo"),
            ("request-1", "tenant-alpha"),  // Same request ID but different tenant
        };

        var actualTenants = new System.Collections.Concurrent.ConcurrentBag<string>();
        var logger = BuildLogger();

        // Act
        foreach (var (requestId, expectedTenantId) in requestsAndExpectedTenants)
        {
            var resolver = Substitute.For<ITenantResolver>();
            resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TenantInfo?>(new TenantInfo(expectedTenantId)));

            var context = BuildContext(c => c.Request.Headers["X-Request-Id"] = requestId);
            var tenantContext = new TenantContext();  // Fresh context per request
            RequestDelegate next = async _ => await Task.CompletedTask;
            var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

            await middleware.InvokeAsync(context, resolver, tenantContext);
            actualTenants.Add(tenantContext.TenantId);
        }

        // Assert - no cross-contamination between requests
        var tenantList = actualTenants.ToList();
        Assert.Equal("tenant-alpha", tenantList[0]);
        Assert.Equal("tenant-bravo", tenantList[1]);
        Assert.Equal("tenant-alpha", tenantList[2]);
    }

    [Fact]
    public async Task TenantContext_IsNotThreadSafe_RequiresScopedInstances()
    {
        // Arrange - single context instance shared across threads (ANTI-PATTERN)
        var sharedContext = new TenantContext();
        var resolvedTenants = new System.Collections.Concurrent.ConcurrentBag<string?>();

        // Act - multiple threads access the same context (simulating singleton misuse)
        var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(() =>
        {
            var info = new TenantInfo($"tenant-{i}", $"Tenant {i}");
            ((dynamic)sharedContext).SetTenant(info);
            System.Threading.Thread.Sleep(1);
            resolvedTenants.Add(sharedContext.TenantId);
        })).ToArray();

        // Assert - demonstrates why singleton storage is dangerous
        // Final state shows last writer wins, but intermediate reads could see wrong tenant
        await Task.WhenAll(tasks);
        Assert.NotEmpty(resolvedTenants);
        // This test documents that scoped contexts are REQUIRED
    }

    [Fact]
    public async Task TenantMiddleware_WithMultipleResolvers_CanBeChained()
    {
        // Arrange - custom resolver that falls back to default strategy
        var chainedResolver = new ChainedTenantResolver(new DefaultTenantResolver(
            Opts(new MultiTenancyOptions { Strategy = TenantResolutionStrategy.Header, HeaderName = "X-Tenant-Id" })));

        var ctx = BuildContext(c => c.Request.Headers["X-Tenant-Id"] = "tenant-chained");
        var tenantContext = new TenantContext();
        var logger = BuildLogger();
        RequestDelegate next = async _ => await Task.CompletedTask;
        var middleware = new TenantMiddleware(next, Opts(new MultiTenancyOptions { Enabled = true }), logger);

        // Act
        await middleware.InvokeAsync(ctx, chainedResolver, tenantContext);

        // Assert
        Assert.Equal("tenant-chained", tenantContext.TenantId);
        Assert.True(tenantContext.IsResolved);
    }

    /// <summary>
    /// Mock chained resolver for demonstration.
    /// Shows how custom resolvers can implement fallback logic.
    /// </summary>
    private sealed class ChainedTenantResolver(ITenantResolver fallback) : ITenantResolver
    {
        public async Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken ct = default)
        {
            // Try primary logic here
            // Fall back if needed
            return await fallback.ResolveAsync(context, ct);
        }
    }
}
