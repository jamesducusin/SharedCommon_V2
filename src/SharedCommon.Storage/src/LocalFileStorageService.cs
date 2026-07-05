using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharedCommon.Storage;

/// <summary>
/// <see cref="IFileStorageService"/> implementation backed by the local file system.
/// Suitable for development and single-node deployments.
/// </summary>
internal sealed class LocalFileStorageService(
    IOptions<StorageOptions> options,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private readonly StorageOptions _options = options.Value;

    private string FullPath(string path) =>
        System.IO.Path.Combine(_options.LocalBasePath, _options.ContainerName, path);

    public async Task<string> SaveAsync(
        string path,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken ct = default)
    {
        var fullPath = FullPath(path);
        var dir = System.IO.Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, ct);

        logger.LogDebug("Saved file {Path} ({ContentType})", path, contentType);
        return path;
    }

    public Task<Stream?> ReadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = FullPath(path);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = FullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogDebug("Deleted file {Path}", path);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(FullPath(path)));

    public Task<IReadOnlyList<StorageFile>> ListAsync(string prefix = "", CancellationToken ct = default)
    {
        var containerPath = System.IO.Path.Combine(_options.LocalBasePath, _options.ContainerName);

        if (!Directory.Exists(containerPath))
            return Task.FromResult<IReadOnlyList<StorageFile>>([]);

        var files = Directory.EnumerateFiles(containerPath, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var relative = System.IO.Path.GetRelativePath(containerPath, f).Replace('\\', '/');
                return string.IsNullOrEmpty(prefix) || relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            })
            .Select(f =>
            {
                var info = new FileInfo(f);
                var relative = System.IO.Path.GetRelativePath(containerPath, f).Replace('\\', '/');
                return new StorageFile(
                    Name: info.Name,
                    Path: relative,
                    SizeBytes: info.Length,
                    ContentType: "application/octet-stream",
                    LastModified: info.LastWriteTimeUtc);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StorageFile>>(files);
    }
}
