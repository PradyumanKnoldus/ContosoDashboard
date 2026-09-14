using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using ContosoDashboard.Tests.Fakes;
using ContosoDashboard.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.US1;

public class DocumentUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_SavesFileMetadataAndAuditForAuthorizedEmployee()
    {
        await using var context = TestDataFactory.CreateContext(nameof(UploadAsync_SavesFileMetadataAndAuditForAuthorizedEmployee));
        var storage = new FakeFileStorageService();
        var scanner = new FakeFileScanService();
        var service = CreateService(context, storage, scanner);
        await using var content = new MemoryStream("safe document"u8.ToArray());

        var result = await service.UploadAsync(4, Request(), content);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Document);
        Assert.Single(storage.Files);
        Assert.Equal("Project Documents", result.Document!.Category);
        Assert.DoesNotContain("report.docx", result.Document.FilePath);
        Assert.Single(context.DocumentActivities);
        Assert.Equal("Upload", context.DocumentActivities.Single().Action);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnauthorizedProjectAssociation()
    {
        await using var context = TestDataFactory.CreateContext(nameof(UploadAsync_RejectsUnauthorizedProjectAssociation));
        context.Users.Add(new User { UserId = 5, Email = "outsider@contoso.com", DisplayName = "Outside User", Role = UserRole.Employee });
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeFileStorageService(), new FakeFileScanService());
        await using var content = new MemoryStream("safe document"u8.ToArray());

        var result = await service.UploadAsync(5, Request(projectId: 1), content);

        Assert.False(result.Succeeded);
        Assert.Contains("not authorized", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAsync_RejectsMalwareBeforeStorage()
    {
        await using var context = TestDataFactory.CreateContext(nameof(UploadAsync_RejectsMalwareBeforeStorage));
        var storage = new FakeFileStorageService();
        var scanner = new FakeFileScanService { IsSafe = false };
        var service = CreateService(context, storage, scanner);
        await using var content = new MemoryStream("malware fixture"u8.ToArray());

        var result = await service.UploadAsync(4, Request(), content);

        Assert.False(result.Succeeded);
        Assert.Empty(storage.Files);
        Assert.Empty(context.Documents);
    }

    [Fact]
    public async Task UploadAsync_RejectsOversizedFilesBeforeStorage()
    {
        await using var context = TestDataFactory.CreateContext(nameof(UploadAsync_RejectsOversizedFilesBeforeStorage));
        var storage = new FakeFileStorageService();
        var service = CreateService(context, storage, new FakeFileScanService());
        await using var content = new MemoryStream(new byte[1]);

        var result = await service.UploadAsync(4, Request(fileSize: DocumentRules.MaxFileSize + 1), content);

        Assert.False(result.Succeeded);
        Assert.Contains("25 MB", result.Error);
        Assert.Empty(storage.Files);
    }

    private static DocumentService CreateService(ApplicationDbContext context, FakeFileStorageService storage, FakeFileScanService scanner)
        => new(context, storage, scanner, new NotificationService(context), NullLogger<DocumentService>.Instance);

    private static UploadRequest Request(int? projectId = null, long fileSize = 128)
        => new("Quarterly report", "Description", "Project Documents", "finance", projectId, null, "report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileSize);
}