namespace SharedCommon.Cloud.UnitTests;

public sealed class CloudOptionsTests
{
    [Fact]
    public void DefaultProvider_IsAzure()
    {
        var opts = new CloudOptions();
        Assert.Equal(CloudProvider.Azure, opts.Provider);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("SharedCommon:Cloud", CloudOptions.SectionName);
    }

    [Fact]
    public void AzureSubOptions_InitializedByDefault()
    {
        var opts = new CloudOptions();
        Assert.NotNull(opts.Azure);
    }

    [Fact]
    public void AwsSubOptions_InitializedByDefault()
    {
        var opts = new CloudOptions();
        Assert.NotNull(opts.Aws);
    }

    [Fact]
    public void CloudProvider_EnumHasBothValues()
    {
        Assert.True(Enum.IsDefined(typeof(CloudProvider), CloudProvider.Azure));
        Assert.True(Enum.IsDefined(typeof(CloudProvider), CloudProvider.AWS));
    }
}

public sealed class AzureCloudOptionsTests
{
    [Fact]
    public void UseManagedIdentity_DefaultsToTrue()
    {
        var opts = new AzureCloudOptions();
        Assert.True(opts.UseManagedIdentity);
    }

    [Fact]
    public void StorageAccountName_DefaultsToNull()
    {
        var opts = new AzureCloudOptions();
        Assert.Null(opts.StorageAccountName);
    }

    [Fact]
    public void KeyVaultUri_DefaultsToNull()
    {
        var opts = new AzureCloudOptions();
        Assert.Null(opts.KeyVaultUri);
    }

    [Fact]
    public void ServiceBusNamespace_DefaultsToNull()
    {
        var opts = new AzureCloudOptions();
        Assert.Null(opts.ServiceBusNamespace);
    }
}

public sealed class AwsCloudOptionsTests
{
    [Fact]
    public void Region_DefaultsToUsEast1()
    {
        var opts = new AwsCloudOptions();
        Assert.Equal("us-east-1", opts.Region);
    }

    [Fact]
    public void AccessKeyId_DefaultsToNull()
    {
        var opts = new AwsCloudOptions();
        Assert.Null(opts.AccessKeyId);
    }

    [Fact]
    public void SecretAccessKey_DefaultsToNull()
    {
        var opts = new AwsCloudOptions();
        Assert.Null(opts.SecretAccessKey);
    }

    [Fact]
    public void ServiceUrl_DefaultsToNull()
    {
        var opts = new AwsCloudOptions();
        Assert.Null(opts.ServiceUrl);
    }
}

public sealed class CloudMessageTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var enqueuedAt = DateTimeOffset.UtcNow;
        var msg = new CloudMessage<string>(
            Payload: "hello",
            MessageId: "msg-1",
            ReceiptHandle: "rh-abc",
            EnqueuedAt: enqueuedAt,
            DeliveryCount: 2);

        Assert.Equal("hello", msg.Payload);
        Assert.Equal("msg-1", msg.MessageId);
        Assert.Equal("rh-abc", msg.ReceiptHandle);
        Assert.Equal(enqueuedAt, msg.EnqueuedAt);
        Assert.Equal(2, msg.DeliveryCount);
    }

    [Fact]
    public void Record_Equality_WorksByValue()
    {
        var at = DateTimeOffset.UtcNow;
        var a = new CloudMessage<int>(42, "id", "rh", at, 1);
        var b = new CloudMessage<int>(42, "id", "rh", at, 1);
        Assert.Equal(a, b);
    }
}
