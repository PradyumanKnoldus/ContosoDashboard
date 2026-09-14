using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.US1;

public class DocumentUploadValidationTests
{
    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    [InlineData(".xlsx")]
    [InlineData(".pptx")]
    [InlineData(".txt")]
    [InlineData(".jpg")]
    [InlineData(".png")]
    public void SupportedExtensions_AreAccepted(string extension)
    {
        Assert.Contains(extension, DocumentRules.Extensions);
    }

    [Fact]
    public void UnsupportedExtension_IsNotAccepted()
    {
        Assert.DoesNotContain(".exe", DocumentRules.Extensions);
    }
}