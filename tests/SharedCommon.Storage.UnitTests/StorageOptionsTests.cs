namespace SharedCommon.Storage.UnitTests;

public sealed class StorageOptionsTests
{
    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("SharedCommon:Storage", StorageOptions.SectionName);
    }

    [Fact]
    public void Provider_DefaultsToLocal()
    {
        var opts = new StorageOptions();
        Assert.Equal(StorageProvider.Local, opts.Provider);
    }

    [Fact]
    public void LocalBasePath_DefaultsToStorageFolder()
    {
        var opts = new StorageOptions();
        Assert.Equal("./storage", opts.LocalBasePath);
    }

    [Fact]
    public void ContainerName_DefaultsToDefault()
    {
        var opts = new StorageOptions();
        Assert.Equal("default", opts.ContainerName);
    }

    [Fact]
    public void StorageProvider_EnumHasBothValues()
    {
        Assert.True(Enum.IsDefined(typeof(StorageProvider), StorageProvider.Local));
        Assert.True(Enum.IsDefined(typeof(StorageProvider), StorageProvider.Cloud));
    }
}

public sealed class StorageFileTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var modified = DateTimeOffset.UtcNow;
        var file = new StorageFile("report.pdf", "reports/report.pdf", 1024, "application/pdf", modified);

        Assert.Equal("report.pdf", file.Name);
        Assert.Equal("reports/report.pdf", file.Path);
        Assert.Equal(1024, file.SizeBytes);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(modified, file.LastModified);
    }

    [Fact]
    public void Record_Equality_WorksByValue()
    {
        var at = DateTimeOffset.UtcNow;
        var a = new StorageFile("a.txt", "a.txt", 10, "text/plain", at);
        var b = new StorageFile("a.txt", "a.txt", 10, "text/plain", at);
        Assert.Equal(a, b);
    }
}
