using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public static class DocumentRules
{
    public const long MaxFileSize = 25 * 1024 * 1024;
    public static readonly IReadOnlySet<string> Categories = new HashSet<string>(StringComparer.Ordinal)
    {
        "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"
    };
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };
}

public sealed record UploadRequest(string Title, string? Description, string Category, string? Tags, int? ProjectId, int? TaskId, string FileName, string ContentType, long FileSize);
public sealed record DocumentResult(bool Succeeded, Document? Document = null, string? Error = null);
public sealed record AuthorizedContent(Stream Content, string ContentType, string FileName);