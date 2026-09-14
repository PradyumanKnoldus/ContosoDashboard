using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public static class DocumentAuthorization
{
    public static bool IsAdministrator(User user) => user.Role == UserRole.Administrator;

    public static bool CanManage(User user, Document document, bool isProjectManager)
        => IsAdministrator(user) || document.UploadedByUserId == user.UserId || isProjectManager;

    public static bool CanView(User user, Document document, bool isProjectMember, bool isProjectManager, bool hasShare)
        => document.DeletedDate is null &&
           (IsAdministrator(user) || document.UploadedByUserId == user.UserId || isProjectMember || isProjectManager || hasShare);
}