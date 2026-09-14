using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Foundational;

public class StorageAndScanTests
{
    [Fact]
    public async Task DeterministicScanner_RejectsMalwareFixture()
    {
        var scanner = new DeterministicFileScanService();
        await using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("EICAR test fixture"));

        var result = await scanner.ScanAsync(content, "fixture.txt", "text/plain");

        Assert.False(result.IsSafe);
    }

    [Fact]
    public async Task DeterministicScanner_AcceptsSafeFixture()
    {
        var scanner = new DeterministicFileScanService();
        await using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("safe fixture"));

        var result = await scanner.ScanAsync(content, "fixture.txt", "text/plain");

        Assert.True(result.IsSafe);
    }
}