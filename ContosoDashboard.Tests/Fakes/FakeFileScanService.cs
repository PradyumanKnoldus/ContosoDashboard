namespace ContosoDashboard.Tests.Fakes;

public sealed class FakeFileScanService : ContosoDashboard.Services.IFileScanService
{
    public bool IsSafe { get; set; } = true;
    public string? Reason { get; set; } = "Configured malware fixture.";

    public Task<ContosoDashboard.Services.FileScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
        => Task.FromResult(new ContosoDashboard.Services.FileScanResult(IsSafe, IsSafe ? null : Reason));
}