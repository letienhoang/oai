namespace OAI.Infrastructure.Tests.Web;

public sealed class InvoiceUploadMarkupTests
{
    [Fact]
    public async Task UploadInput_AcceptsImagesPdfAndZip()
    {
        var uploadPage = await File.ReadAllTextAsync(FindRepositoryFile(
            "OAI.Web",
            "Components",
            "Pages",
            "Invoices",
            "InvoiceUpload.razor"));

        Assert.Contains(
            "accept=\"image/*,.pdf,application/pdf,.zip,application/zip,application/x-zip-compressed\"",
            uploadPage);
    }

    [Fact]
    public async Task MobileCaptureMarkup_ContainsMobileUploadRouteAndControls()
    {
        var mobileCapturePage = await File.ReadAllTextAsync(FindRepositoryFile(
            "OAI.Web",
            "Components",
            "Pages",
            "Mobile",
            "MobileCapture.razor"));

        Assert.Contains("@page \"/mobile/capture\"", mobileCapturePage);
        Assert.Contains("MobileCapturePageTitle", mobileCapturePage);
        Assert.Contains("<InputFile", mobileCapturePage);
        Assert.Contains("accept=\"image/*,.pdf,application/pdf\"", mobileCapturePage);
        Assert.DoesNotContain(".zip", mobileCapturePage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MobileCaptureSupportedFormats", mobileCapturePage);
        Assert.Contains("MobileCaptureUpload", mobileCapturePage);
    }

    private static string FindRepositoryFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                new[] { directory.FullName }.Concat(pathSegments).ToArray());

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find repository file '{Path.Combine(pathSegments)}'.");
    }
}
