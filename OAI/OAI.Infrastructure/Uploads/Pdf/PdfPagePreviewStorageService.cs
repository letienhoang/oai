using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Application.Uploads.Pdf;
using OAI.Domain.Entities;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Uploads.Pdf;

public sealed class PdfPagePreviewStorageService : IPdfPagePreviewStorageService
{
    private const string PngContentType = "image/png";

    private readonly OaiDbContext _dbContext;
    private readonly FileStorageOptions _options;
    private readonly string _basePath;
    private readonly ILogger<PdfPagePreviewStorageService> _logger;

    public PdfPagePreviewStorageService(
        OaiDbContext dbContext,
        IOptions<FileStorageOptions> options,
        ILogger<PdfPagePreviewStorageService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _basePath = GetBasePath(_options.BasePath);
        _logger = logger;
    }

    public async Task<PdfPagePreviewStorageResult> StoreAsync(
        Guid uploadBatchFileId,
        IReadOnlyList<PdfRenderedPageResult> renderedPages,
        CancellationToken cancellationToken = default)
    {
        if (uploadBatchFileId == Guid.Empty)
        {
            return PdfPagePreviewStorageResult.Failed(
                "PDF page preview storage failed because UploadBatchFileId is empty.");
        }

        if (renderedPages is null || renderedPages.Count == 0)
        {
            return PdfPagePreviewStorageResult.Failed(
                "PDF page preview storage failed because there are no rendered pages to store.");
        }

        var storageRootPath = NormalizeConfiguredRootPath(_options.RootPath);
        if (storageRootPath is null)
        {
            return PdfPagePreviewStorageResult.Failed(
                "PDF page preview storage failed because the configured storage root path is unsafe.");
        }

        var uploadBatchFile = await _dbContext.UploadBatchFiles
            .FirstOrDefaultAsync(x => x.Id == uploadBatchFileId, cancellationToken);

        if (uploadBatchFile is null)
        {
            return PdfPagePreviewStorageResult.Failed(
                $"PDF page preview storage failed because upload batch file '{uploadBatchFileId}' was not found.");
        }

        var duplicatePage = renderedPages
            .GroupBy(x => x.PageNumber)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicatePage is not null)
        {
            return PdfPagePreviewStorageResult.Failed(
                $"PDF page preview storage failed because page {duplicatePage.Key} was rendered more than once.");
        }

        var previewRelativeDirectory = Path.Combine(
            storageRootPath,
            "batches",
            uploadBatchFile.UploadBatchId.ToString("N"),
            "files",
            uploadBatchFile.Id.ToString("N"),
            "previews");

        var previewPhysicalDirectory = Path.Combine(_basePath, previewRelativeDirectory);
        var storagePhysicalRoot = Path.GetFullPath(Path.Combine(_basePath, storageRootPath));

        if (!IsUnderRoot(previewPhysicalDirectory, storagePhysicalRoot))
        {
            return PdfPagePreviewStorageResult.Failed(
                "PDF page preview storage failed because the preview directory resolves outside the configured storage root.");
        }

        Directory.CreateDirectory(previewPhysicalDirectory);

        var storedPreviews = new List<PdfStoredPagePreviewResult>(renderedPages.Count);

        foreach (var renderedPage in renderedPages.OrderBy(x => x.PageNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validationError = ValidateRenderedPage(renderedPage);
            if (validationError is not null)
            {
                return PdfPagePreviewStorageResult.Failed(validationError);
            }

            var sourcePhysicalPath = Path.GetFullPath(renderedPage.ImagePath);
            if (!File.Exists(sourcePhysicalPath))
            {
                return PdfPagePreviewStorageResult.Failed(
                    $"PDF page preview storage failed because rendered page file was not found: {renderedPage.ImagePath}");
            }

            var previewFileName = $"page-{renderedPage.PageNumber:000}.png";
            var previewPhysicalPath = Path.GetFullPath(Path.Combine(previewPhysicalDirectory, previewFileName));
            if (!IsUnderRoot(previewPhysicalPath, storagePhysicalRoot))
            {
                return PdfPagePreviewStorageResult.Failed(
                    "PDF page preview storage failed because the preview file path resolves outside the configured storage root.");
            }

            var existingPreview = await _dbContext.InvoiceSourceFiles
                .FirstOrDefaultAsync(
                    x => x.UploadBatchFileId == uploadBatchFile.Id &&
                         x.PageNumber == renderedPage.PageNumber,
                    cancellationToken);

            if (File.Exists(previewPhysicalPath) && existingPreview is null)
            {
                return PdfPagePreviewStorageResult.Failed(
                    $"PDF page preview storage failed because the preview file already exists without matching metadata: {ToRelativePath(previewPhysicalPath)}");
            }

            await CopyFileAsync(
                sourcePhysicalPath,
                previewPhysicalPath,
                overwrite: existingPreview is not null,
                cancellationToken);

            var storedFileInfo = new FileInfo(previewPhysicalPath);
            var previewRelativePath = NormalizePath(Path.Combine(previewRelativeDirectory, previewFileName));

            if (existingPreview is null)
            {
                var sourceFile = new InvoiceSourceFile(
                    invoiceId: null,
                    originalFileName: uploadBatchFile.OriginalFileName,
                    storedFilePath: uploadBatchFile.StoredFilePath,
                    contentType: PngContentType,
                    fileSizeBytes: storedFileInfo.Length,
                    uploadBatchFileId: uploadBatchFile.Id,
                    previewFilePath: previewRelativePath,
                    pageNumber: renderedPage.PageNumber);

                await _dbContext.InvoiceSourceFiles.AddAsync(sourceFile, cancellationToken);
            }
            else
            {
                existingPreview.UpdatePreviewMetadata(
                    previewRelativePath,
                    PngContentType,
                    storedFileInfo.Length,
                    renderedPage.PageNumber);
            }

            storedPreviews.Add(new PdfStoredPagePreviewResult(
                renderedPage.PageNumber,
                previewRelativePath,
                PngContentType,
                storedFileInfo.Length));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stored PDF page previews. UploadBatchFileId: {UploadBatchFileId}, PreviewCount: {PreviewCount}",
            uploadBatchFile.Id,
            storedPreviews.Count);

        return new PdfPagePreviewStorageResult(
            Succeeded: true,
            Previews: storedPreviews,
            ErrorMessage: null);
    }

    private static string? ValidateRenderedPage(PdfRenderedPageResult renderedPage)
    {
        if (renderedPage.PageNumber <= 0)
        {
            return "PDF page preview storage failed because rendered page numbers must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(renderedPage.ImagePath))
        {
            return $"PDF page preview storage failed because rendered page {renderedPage.PageNumber} has no image path.";
        }

        if (!string.Equals(renderedPage.ContentType, PngContentType, StringComparison.OrdinalIgnoreCase))
        {
            return $"PDF page preview storage failed because rendered page {renderedPage.PageNumber} is not a PNG image.";
        }

        return null;
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        await using var destination = new FileStream(
            destinationPath,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await source.CopyToAsync(destination, cancellationToken);
    }

    private static string GetBasePath(string? configuredBasePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredBasePath))
        {
            return Path.GetFullPath(configuredBasePath);
        }

        return AppContext.BaseDirectory;
    }

    private static string? NormalizeConfiguredRootPath(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return "storage";
        }

        var normalized = rootPath.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized))
        {
            return null;
        }

        var segments = normalized.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Any(segment =>
                segment == "." ||
                segment == ".." ||
                segment.Contains(':') ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            return null;
        }

        return Path.Combine(segments);
    }

    private static bool IsUnderRoot(string path, string rootPath)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(rootPath);

        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRoot += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');

    private string ToRelativePath(string physicalPath)
    {
        var relativePath = Path.GetRelativePath(_basePath, physicalPath);
        return NormalizePath(relativePath);
    }
}
