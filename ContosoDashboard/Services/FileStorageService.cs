namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredRoot = configuration["DocumentStorage:RootPath"];
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(environment.ContentRootPath, "AppData", "uploads")
            : Path.IsPathRooted(configuredRoot) ? configuredRoot : Path.Combine(environment.ContentRootPath, configuredRoot));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        Stream? result = File.Exists(fullPath) ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(ResolvePath(relativePath)));

    private string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new ArgumentException("Storage paths must be non-empty relative paths.", nameof(relativePath));

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Storage path escapes the configured root.", nameof(relativePath));

        return fullPath;
    }
}