namespace ContosoDashboard.Services;

public sealed record FileScanResult(bool IsSafe, string? Reason = null);

public interface IFileScanService
{
    Task<FileScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

public sealed class DeterministicFileScanService : IFileScanService
{
    public async Task<FileScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (content is null) return new(false, "File content was not supplied.");
        if (content.CanSeek) content.Position = 0;
        using var reader = new StreamReader(content, leaveOpen: true);
        var sample = await reader.ReadToEndAsync(cancellationToken);
        if (content.CanSeek) content.Position = 0;

        return sample.Contains("EICAR", StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains("malware", StringComparison.OrdinalIgnoreCase)
            ? new FileScanResult(false, "The file failed the malware scan.")
            : new FileScanResult(true);
    }
}