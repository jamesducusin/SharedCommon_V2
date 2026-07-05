namespace SharedCommon.Auditing.UnitTests;

public sealed class AuditEntryTests
{
    [Fact]
    public void AuditEntry_DefaultId_IsNotEmpty()
    {
        var entry = new AuditEntry();
        Assert.NotEqual(Guid.Empty, entry.Id);
    }

    [Fact]
    public void AuditEntry_DefaultOccurredAt_IsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var entry = new AuditEntry();
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(entry.OccurredAt, before, after);
    }

    [Fact]
    public void AuditEntry_DefaultChangedProperties_IsEmpty() =>
        Assert.Empty(new AuditEntry().ChangedProperties);

    [Fact]
    public void AuditEntry_DefaultMetadata_IsCaseInsensitiveDictionary()
    {
        var entry = new AuditEntry();
        entry.Metadata["Key"] = "value";
        Assert.Equal("value", entry.Metadata["key"]);
    }

    [Theory]
    [InlineData(AuditAction.Created)]
    [InlineData(AuditAction.Updated)]
    [InlineData(AuditAction.Deleted)]
    [InlineData(AuditAction.SoftDeleted)]
    [InlineData(AuditAction.Accessed)]
    public void AuditAction_AllValuesExist(AuditAction action) =>
        Assert.True(Enum.IsDefined(action));
}
