using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Foundational;

public class DocumentFoundationTests
{
    [Fact]
    public void Rules_UseRequiredCategoriesAndTwentyFiveMegabyteLimit()
    {
        Assert.Equal(25 * 1024 * 1024, DocumentRules.MaxFileSize);
        Assert.Contains("Project Documents", DocumentRules.Categories);
        Assert.Contains("Personal Files", DocumentRules.Categories);
        Assert.DoesNotContain("Public Files", DocumentRules.Categories);
        Assert.Contains(".docx", DocumentRules.Extensions);
        Assert.Contains(".png", DocumentRules.Extensions);
    }

    [Fact]
    public void Authorization_AllowsOwnerAndAdministratorOnlyForUnsharedPersonalDocument()
    {
        var document = new Document { UploadedByUserId = 4 };
        var owner = new User { UserId = 4, Role = UserRole.Employee };
        var administrator = new User { UserId = 1, Role = UserRole.Administrator };
        var other = new User { UserId = 3, Role = UserRole.TeamLead };

        Assert.True(DocumentAuthorization.CanView(owner, document, false, false, false));
        Assert.True(DocumentAuthorization.CanView(administrator, document, false, false, false));
        Assert.False(DocumentAuthorization.CanView(other, document, false, false, false));
    }
}