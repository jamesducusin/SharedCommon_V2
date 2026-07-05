using System.Text;

namespace SharedCommon.Storage.UnitTests;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private LocalFileStorageService BuildService(string container = "test")
    {
        var opts = Options.Create(new StorageOptions
        {
            Provider = StorageProvider.Local,
            LocalBasePath = _tempDir,
            ContainerName = container
        });
        return new LocalFileStorageService(opts, NullLogger<LocalFileStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_CreatesFile_AndReturnsPath()
    {
        var svc = BuildService();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));

        var path = await svc.SaveAsync("docs/hello.txt", content, "text/plain");

        Assert.Equal("docs/hello.txt", path);
        Assert.True(await svc.ExistsAsync("docs/hello.txt"));
    }

    [Fact]
    public async Task ReadAsync_ReturnsStream_WhenFileExists()
    {
        var svc = BuildService();
        var data = Encoding.UTF8.GetBytes("read me");
        await svc.SaveAsync("read.txt", new MemoryStream(data));

        await using var stream = await svc.ReadAsync("read.txt");

        Assert.NotNull(stream);
        var result = await new StreamReader(stream!).ReadToEndAsync();
        Assert.Equal("read me", result);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNull_WhenFileMissing()
    {
        var svc = BuildService();
        var stream = await svc.ReadAsync("nonexistent.txt");
        Assert.Null(stream);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenFileMissing()
    {
        var svc = BuildService();
        Assert.False(await svc.ExistsAsync("ghost.txt"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        var svc = BuildService();
        await svc.SaveAsync("delete-me.txt", new MemoryStream(Encoding.UTF8.GetBytes("bye")));
        Assert.True(await svc.ExistsAsync("delete-me.txt"));

        await svc.DeleteAsync("delete-me.txt");

        Assert.False(await svc.ExistsAsync("delete-me.txt"));
    }

    [Fact]
    public async Task DeleteAsync_IsNoOp_WhenFileMissing()
    {
        var svc = BuildService();
        var ex = await Record.ExceptionAsync(() => svc.DeleteAsync("does-not-exist.txt"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllFiles_WhenNoPrefixGiven()
    {
        var svc = BuildService();
        await svc.SaveAsync("a.txt", new MemoryStream(Encoding.UTF8.GetBytes("a")));
        await svc.SaveAsync("sub/b.txt", new MemoryStream(Encoding.UTF8.GetBytes("b")));

        var files = await svc.ListAsync();

        Assert.Equal(2, files.Count);
    }

    [Fact]
    public async Task ListAsync_FiltersBy_Prefix()
    {
        var svc = BuildService();
        await svc.SaveAsync("images/cat.png", new MemoryStream(Encoding.UTF8.GetBytes("img")));
        await svc.SaveAsync("docs/report.pdf", new MemoryStream(Encoding.UTF8.GetBytes("doc")));

        var files = await svc.ListAsync("images/");

        Assert.Single(files);
        Assert.Equal("cat.png", files[0].Name);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenContainerDoesNotExist()
    {
        var svc = BuildService("nonexistent-container");
        var files = await svc.ListAsync();
        Assert.Empty(files);
    }

    [Fact]
    public async Task SaveAsync_Overwrites_ExistingFile()
    {
        var svc = BuildService();
        await svc.SaveAsync("file.txt", new MemoryStream(Encoding.UTF8.GetBytes("v1")));
        await svc.SaveAsync("file.txt", new MemoryStream(Encoding.UTF8.GetBytes("v2")));

        await using var stream = await svc.ReadAsync("file.txt");
        var content = await new StreamReader(stream!).ReadToEndAsync();
        Assert.Equal("v2", content);
    }
}
