namespace ContosoDashboard.Tests.Fakes;

public sealed class FakeFileStorageService : ContosoDashboard.Services.IFileStorageService
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool FailUploads { get; set; }
    public bool FailDeletes { get; set; }

    public async Task<string> UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken = default)
    {
        if (FailUploads) throw new IOException("Configured upload failure.");
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Files.Add(relativePath, buffer.ToArray());
        return relativePath;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (FailDeletes) throw new IOException("Configured delete failure.");
        Files.Remove(relativePath);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        Stream? stream = Files.TryGetValue(relativePath, out var bytes) ? new MemoryStream(bytes, writable: false) : null;
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => Task.FromResult(Files.ContainsKey(relativePath));
}