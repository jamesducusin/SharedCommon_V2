namespace SharedCommon.MultiTenancy.UnitTests;

/// <summary>
/// Behavioral tests for TenantContext.
/// Verifies mutable state management, isolation, and contract compliance.
/// </summary>
public sealed class TenantContextBehaviorTests
{
    [Fact]
    public void TenantContext_InitiallyUnresolved()
    {
        // Arrange & Act
        var ctx = new TenantContext();

        // Assert
        Assert.Empty(ctx.TenantId);
        Assert.Null(ctx.TenantName);
        Assert.False(ctx.IsResolved);
        Assert.NotNull(ctx.Properties);
        Assert.Empty(ctx.Properties);
    }

    [Fact]
    public void TenantContext_SetTenant_UpdatesAllProperties()
    {
        // Arrange
        var ctx = new TenantContext();
        var props = new Dictionary<string, string> { ["Plan"] = "Pro" };
        var info = new TenantInfo("t-42", "Tenant42", props);

        // Act
        ((dynamic)ctx).SetTenant(info);  // Internal method

        // Assert
        Assert.Equal("t-42", ctx.TenantId);
        Assert.Equal("Tenant42", ctx.TenantName);
        Assert.True(ctx.IsResolved);
        Assert.Single(ctx.Properties);
        Assert.Equal("Pro", ctx.Properties["Plan"]);
    }

    [Fact]
    public void TenantContext_SetTenant_WithNullProperties_UsesEmpty()
    {
        // Arrange
        var ctx = new TenantContext();
        var info = new TenantInfo("t-no-props", "Tenant");

        // Act
        ((dynamic)ctx).SetTenant(info);

        // Assert
        Assert.NotNull(ctx.Properties);
        Assert.Empty(ctx.Properties);
    }

    [Fact]
    public void TenantContext_PropertiesAreReadOnly_FromPublicInterface()
    {
        // Arrange
        var ctx = new TenantContext();
        var info = new TenantInfo("t-1", "T1", new Dictionary<string, string> { ["X"] = "Y" });
        ((dynamic)ctx).SetTenant(info);

        // Act & Assert - Properties should be IReadOnlyDictionary
        var properties = ctx.Properties;
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(properties);
        Assert.Equal("Y", properties["X"]);
        Assert.Single(properties);
    }

    [Fact]
    public void TenantContext_MultipleSetTenant_Overwrites()
    {
        // Arrange
        var ctx = new TenantContext();
        var info1 = new TenantInfo("t-1", "Tenant1");
        var info2 = new TenantInfo("t-2", "Tenant2");

        // Act
        ((dynamic)ctx).SetTenant(info1);
        ((dynamic)ctx).SetTenant(info2);

        // Assert - second should win
        Assert.Equal("t-2", ctx.TenantId);
        Assert.Equal("Tenant2", ctx.TenantName);
    }

    [Fact]
    public void TenantContext_CanBeCastToITenantContext()
    {
        // Arrange & Act
        var ctx = new TenantContext();
        ITenantContext iface = ctx;

        // Assert
        Assert.NotNull(iface);
        Assert.False(iface.IsResolved);
    }

    [Fact]
    public void TenantContext_EmptyPropertiesDictionary_IsConsistentAcrossCalls()
    {
        // Arrange
        var ctx1 = new TenantContext();
        var ctx2 = new TenantContext();

        // Act
        var props1 = ctx1.Properties;
        var props2 = ctx2.Properties;

        // Assert - both should be empty but may be same singleton
        Assert.Empty(props1);
        Assert.Empty(props2);
    }

    [Fact]
    public void TenantContext_PreservesPropertyCasing()
    {
        // Arrange
        var ctx = new TenantContext();
        var props = new Dictionary<string, string>
        {
            ["CustomPlan"] = "premium",
            ["customPlan"] = "basic",  // Different casing
            ["CUSTOM_PLAN"] = "enterprise",
        };
        var info = new TenantInfo("t-casing", "T", props);

        // Act
        ((dynamic)ctx).SetTenant(info);

        // Assert
        Assert.Equal(3, ctx.Properties.Count);
        Assert.Equal("premium", ctx.Properties["CustomPlan"]);
        Assert.Equal("basic", ctx.Properties["customPlan"]);
        Assert.Equal("enterprise", ctx.Properties["CUSTOM_PLAN"]);
    }

    [Fact]
    public void TenantContext_CanAccessPropertiesMultipleTimes()
    {
        // Arrange
        var ctx = new TenantContext();
        var props = new Dictionary<string, string> { ["Key1"] = "Value1", ["Key2"] = "Value2" };
        var info = new TenantInfo("t-multi", "T", props);
        ((dynamic)ctx).SetTenant(info);

        // Act
        var props1 = ctx.Properties;
        var props2 = ctx.Properties;
        var props3 = ctx.Properties;

        // Assert - should return consistent values
        Assert.Equal("Value1", props1["Key1"]);
        Assert.Equal("Value1", props2["Key1"]);
        Assert.Equal("Value1", props3["Key1"]);
    }
}

/// <summary>
/// Behavioral tests for tenant context isolation across concurrent requests.
/// Verifies scoped isolation in multi-threaded scenarios.
/// </summary>
public sealed class TenantContextIsolationTests
{
    [Fact]
    public void TenantContext_MultipleInstances_AreIndependent()
    {
        // Arrange
        var ctx1 = new TenantContext();
        var ctx2 = new TenantContext();

        var info1 = new TenantInfo("t-1", "Tenant1");
        var info2 = new TenantInfo("t-2", "Tenant2");

        // Act
        ((dynamic)ctx1).SetTenant(info1);
        ((dynamic)ctx2).SetTenant(info2);

        // Assert - they should not interfere
        Assert.Equal("t-1", ctx1.TenantId);
        Assert.Equal("t-2", ctx2.TenantId);
        Assert.NotEqual(ctx1.TenantId, ctx2.TenantId);
    }

    [Fact]
    public async Task TenantContext_InDifferentAsyncScopes_AreIsolated()
    {
        // Arrange - simulate scoped DI in async contexts
        var task1Tenant = string.Empty;
        var task2Tenant = string.Empty;

        var task1 = Task.Run(() =>
        {
            var ctx = new TenantContext();
            ((dynamic)ctx).SetTenant(new TenantInfo("tenant-task-1"));
            task1Tenant = ctx.TenantId;
        });

        var task2 = Task.Run(() =>
        {
            var ctx = new TenantContext();
            ((dynamic)ctx).SetTenant(new TenantInfo("tenant-task-2"));
            task2Tenant = ctx.TenantId;
        });

        // Act
        await Task.WhenAll(task1, task2);

        // Assert
        Assert.Equal("tenant-task-1", task1Tenant);
        Assert.Equal("tenant-task-2", task2Tenant);
    }

    [Fact]
    public void TenantContext_SetTenant_IsIdempotent_ForSameTenant()
    {
        // Arrange
        var ctx = new TenantContext();
        var info = new TenantInfo("t-idem", "Tenant", new Dictionary<string, string> { ["X"] = "Y" });

        // Act
        ((dynamic)ctx).SetTenant(info);
        var tenantIdAfterFirst = ctx.TenantId;
        var isResolvedAfterFirst = ctx.IsResolved;

        ((dynamic)ctx).SetTenant(info);
        var tenantIdAfterSecond = ctx.TenantId;
        var isResolvedAfterSecond = ctx.IsResolved;

        // Assert - should be the same
        Assert.Equal(tenantIdAfterFirst, tenantIdAfterSecond);
        Assert.Equal(isResolvedAfterFirst, isResolvedAfterSecond);
    }
}
