using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<DocumentResult> UploadAsync(int requestingUserId, UploadRequest request, Stream content, CancellationToken cancellationToken = default);
    Task<AuthorizedContent?> OpenContentAsync(int documentId, int requestingUserId, bool preview, CancellationToken cancellationToken = default);
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IFileScanService _scanner;
    private readonly INotificationService _notifications;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService storage,
        IFileScanService scanner,
        INotificationService notifications,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storage = storage;
        _scanner = scanner;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<DocumentResult> UploadAsync(int requestingUserId, UploadRequest request, Stream content, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request, content);
        if (validationError is not null) return new(false, Error: validationError);

        var user = await _context.Users.FindAsync([requestingUserId], cancellationToken);
        if (user is null) return new(false, Error: "The current user could not be found.");

        var project = request.ProjectId.HasValue
            ? await _context.Projects.Include(p => p.ProjectMembers).FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId.Value, cancellationToken)
            : null;
        if (request.ProjectId.HasValue && project is null) return new(false, Error: "The selected project is not available.");
        var projectAccess = project is not null && (project.ProjectManagerId == requestingUserId || project.ProjectMembers.Any(m => m.UserId == requestingUserId) || DocumentAuthorization.IsAdministrator(user));
        if (request.ProjectId.HasValue && !projectAccess) return new(false, Error: "You are not authorized to upload to this project.");

        if (request.TaskId.HasValue)
        {
            var task = await _context.Tasks.Include(t => t.Project).ThenInclude(p => p!.ProjectMembers).FirstOrDefaultAsync(t => t.TaskId == request.TaskId.Value, cancellationToken);
            if (task is null || (request.ProjectId.HasValue && task.ProjectId != request.ProjectId) ||
                (task.AssignedUserId != requestingUserId && task.CreatedByUserId != requestingUserId &&
                 task.Project?.ProjectManagerId != requestingUserId && !(task.Project?.ProjectMembers.Any(m => m.UserId == requestingUserId) ?? false) && !DocumentAuthorization.IsAdministrator(user)))
                return new(false, Error: "You are not authorized to attach a document to this task.");
        }

        if (content.CanSeek) content.Position = 0;
        var scan = await _scanner.ScanAsync(content, request.FileName, request.ContentType, cancellationToken);
        if (!scan.IsSafe) return new(false, Error: scan.Reason ?? "The file failed the malware scan.");
        if (content.CanSeek) content.Position = 0;

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var scope = request.ProjectId?.ToString() ?? "personal";
        var relativePath = $"{requestingUserId}/{scope}/{Guid.NewGuid():N}{extension}";
        var stored = false;
        try
        {
            await _storage.UploadAsync(content, relativePath, request.ContentType, cancellationToken);
            stored = true;

            var now = DateTime.UtcNow;
            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Category = request.Category,
                Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim(),
                FileName = Path.GetFileName(request.FileName),
                FilePath = relativePath,
                FileType = request.ContentType,
                FileSize = request.FileSize,
                UploadedByUserId = requestingUserId,
                ProjectId = request.ProjectId,
                TaskId = request.TaskId,
                UploadedDate = now,
                UpdatedDate = now
            };
            _context.Documents.Add(document);
            _context.DocumentActivities.Add(new DocumentActivity
            {
                Document = document,
                ActorUserId = requestingUserId,
                Action = "Upload",
                OccurredDate = now,
                RetainUntil = now.AddYears(7)
            });
            await _context.SaveChangesAsync(cancellationToken);

            if (project is not null)
            {
                foreach (var member in project.ProjectMembers.Where(m => m.UserId != requestingUserId).Select(m => m.UserId).Distinct())
                {
                    try
                    {
                        await _notifications.CreateNotificationAsync(new Notification
                        {
                            UserId = member,
                            Title = "New Project Document",
                            Message = $"A new document was added to {project.Name}.",
                            Type = NotificationType.ProjectUpdate,
                            Priority = NotificationPriority.Informational
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to notify user {UserId} about document upload {DocumentId}.", member, document.DocumentId);
                    }
                }
            }

            return new(true, document);
        }
        catch (Exception ex)
        {
            if (stored)
            {
                try { await _storage.DeleteAsync(relativePath, cancellationToken); }
                catch (Exception cleanupException) { _logger.LogError(cleanupException, "Unable to clean up failed document path."); }
            }
            _logger.LogError(ex, "Document upload failed for user {UserId}.", requestingUserId);
            return new(false, Error: "The document could not be saved.");
        }
    }

    public async Task<AuthorizedContent?> OpenContentAsync(int documentId, int requestingUserId, bool preview, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .Include(d => d.Project).ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.Task).ThenInclude(t => t!.Project).ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.Shares)
            .Include(d => d.UploadedByUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document is null) return null;

        var user = await _context.Users.FindAsync([requestingUserId], cancellationToken);
        if (user is null) return null;
        var isMember = document.Project?.ProjectMembers.Any(m => m.UserId == requestingUserId) == true || document.Task?.Project?.ProjectMembers.Any(m => m.UserId == requestingUserId) == true;
        var isManager = document.Project?.ProjectManagerId == requestingUserId || document.Task?.Project?.ProjectManagerId == requestingUserId;
        var hasShare = document.Shares.Any(s => s.RevokedDate is null && s.RecipientUserId == requestingUserId);
        if (!DocumentAuthorization.CanView(user, document, isMember, isManager, hasShare)) return null;

        var stream = await _storage.OpenReadAsync(document.FilePath, cancellationToken);
        if (stream is null) return null;
        var now = DateTime.UtcNow;
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = requestingUserId,
            Action = preview ? "Preview" : "Download",
            OccurredDate = now,
            RetainUntil = now.AddYears(7)
        });
        await _context.SaveChangesAsync(cancellationToken);
        return new AuthorizedContent(stream, document.FileType, document.FileName);
    }

    private static string? Validate(UploadRequest request, Stream content)
    {
        if (content is null || request.FileSize <= 0) return "The file must not be empty.";
        if (request.FileSize > DocumentRules.MaxFileSize) return "Each file must be 25 MB or smaller.";
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 255) return "A document title of 1 to 255 characters is required.";
        if (!DocumentRules.Categories.Contains(request.Category)) return "Select a valid document category.";
        var extension = Path.GetExtension(request.FileName);
        if (!DocumentRules.Extensions.Contains(extension)) return "This file type is not supported.";
        if (string.IsNullOrWhiteSpace(request.ContentType) || request.ContentType.Length > 255) return "The file type is invalid.";
        return null;
    }
}