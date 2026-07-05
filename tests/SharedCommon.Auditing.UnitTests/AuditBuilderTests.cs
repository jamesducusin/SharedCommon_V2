namespace SharedCommon.Auditing.UnitTests;

public sealed class AuditBuilderTests
{
    [Fact]
    public void For_SetsEntityTypeAndId()
    {
        var entry = AuditBuilder.For("Order", "42").Action(AuditAction.Created).Build();
        Assert.Equal("Order", entry.EntityType);
        Assert.Equal("42", entry.EntityId);
    }

    [Fact]
    public void Action_SetsAction()
    {
        var entry = AuditBuilder.For("Order", "1").Action(AuditAction.Deleted).Build();
        Assert.Equal(AuditAction.Deleted, entry.Action);
    }

    [Fact]
    public void By_SetsUserId()
    {
        var entry = AuditBuilder.For("Order", "1").By("user-123").Build();
        Assert.Equal("user-123", entry.UserId);
    }

    [Fact]
    public void ForTenant_SetsTenantId()
    {
        var entry = AuditBuilder.For("Order", "1").ForTenant("tenant-abc").Build();
        Assert.Equal("tenant-abc", entry.TenantId);
    }

    [Fact]
    public void WithCorrelation_String_SetsCorrelationId()
    {
        var entry = AuditBuilder.For("Order", "1").WithCorrelation("corr-xyz").Build();
        Assert.Equal("corr-xyz", entry.CorrelationId);
    }

    [Fact]
    public void WithCorrelation_CorrelationId_SetsCorrelationId()
    {
        var corrId = CorrelationId.New();
        var entry = AuditBuilder.For("Order", "1").WithCorrelation(corrId).Build();
        Assert.Equal(corrId.Value, entry.CorrelationId);
    }

    [Fact]
    public void OldValues_SetsOldValues()
    {
        var entry = AuditBuilder.For("Order", "1").OldValues("{\"status\":\"Pending\"}").Build();
        Assert.Equal("{\"status\":\"Pending\"}", entry.OldValues);
    }

    [Fact]
    public void NewValues_SetsNewValues()
    {
        var entry = AuditBuilder.For("Order", "1").NewValues("{\"status\":\"Shipped\"}").Build();
        Assert.Equal("{\"status\":\"Shipped\"}", entry.NewValues);
    }

    [Fact]
    public void Changed_AddsChangedProperties()
    {
        var entry = AuditBuilder.For("Order", "1").Changed("Status", "Total").Build();
        Assert.Contains("Status", entry.ChangedProperties);
        Assert.Contains("Total", entry.ChangedProperties);
    }

    [Fact]
    public void WithMetadata_AddsMetadataKeyValue()
    {
        var entry = AuditBuilder.For("Order", "1").WithMetadata("ip", "127.0.0.1").Build();
        Assert.Equal("127.0.0.1", entry.Metadata["ip"]);
    }

    [Fact]
    public void WithMetadata_KeyIsCaseInsensitive()
    {
        var entry = AuditBuilder.For("Order", "1").WithMetadata("IP", "127.0.0.1").Build();
        Assert.Equal("127.0.0.1", entry.Metadata["ip"]);
    }

    [Fact]
    public void FromContext_CopiesUserTenantCorrelation()
    {
        var context = new RequestContext
        {
            UserId = "user-1",
            TenantId = "tenant-1",
            CorrelationId = CorrelationId.New()
        };
        var entry = AuditBuilder.For("Order", "1").FromContext(context).Build();
        Assert.Equal("user-1", entry.UserId);
        Assert.Equal("tenant-1", entry.TenantId);
        Assert.Equal(context.CorrelationId.Value, entry.CorrelationId);
    }

    [Fact]
    public void Build_DefaultEntry_HasNewIdAndTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var entry = AuditBuilder.For("X", "1").Build();
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.InRange(entry.OccurredAt, before, after);
    }

    [Fact]
    public void Build_FluentChain_AllFieldsSet()
    {
        var corrId = CorrelationId.New();
        var entry = AuditBuilder
            .For("Invoice", "INV-001")
            .Action(AuditAction.Updated)
            .By("admin")
            .ForTenant("acme")
            .WithCorrelation(corrId)
            .OldValues("{}")
            .NewValues("{\"amount\":100}")
            .Changed("Amount")
            .WithMetadata("source", "api")
            .Build();

        Assert.Equal("Invoice", entry.EntityType);
        Assert.Equal("INV-001", entry.EntityId);
        Assert.Equal(AuditAction.Updated, entry.Action);
        Assert.Equal("admin", entry.UserId);
        Assert.Equal("acme", entry.TenantId);
        Assert.Equal(corrId.Value, entry.CorrelationId);
        Assert.Equal("{}", entry.OldValues);
        Assert.Equal("{\"amount\":100}", entry.NewValues);
        Assert.Contains("Amount", entry.ChangedProperties);
        Assert.Equal("api", entry.Metadata["source"]);
    }
}
