using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OAI.Application.Uploads.Pdf;
using OAI.Domain.Entities;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;
using OAI.Infrastructure.Uploads.Pdf;

namespace OAI.Infrastructure.Tests.Uploads.Pdf;

public sealed class PdfPagePreviewStorageServiceTests
{
    [Fact]
    public async Task StoreAsync_StoresOneRenderedPagePreview()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var uploadBatchFile = await AddUploadBatchFileAsync(dbContext);
            var sourcePath = CreateRenderedPage(root, "rendered-page.png", [0x01, 0x02, 0x03]);
            var service = CreateService(dbContext, root);

            var result = await service.StoreAsync(
                uploadBatchFile.Id,
                [CreateRenderedPageResult(1, sourcePath)]);

            Assert.True(result.Succeeded);
            var preview = Assert.Single(result.Previews);
            Assert.Equal(1, preview.PageNumber);
            Assert.Equal("image/png", preview.ContentType);
            Assert.Equal(3, preview.FileSizeBytes);
            Assert.Equal(ExpectedPreviewPath(uploadBatchFile, 1), preview.PreviewFilePath);
            Assert.True(File.Exists(Path.Combine(root, preview.PreviewFilePath)));

            var storedSourceFile = Assert.Single(await dbContext.InvoiceSourceFiles.ToListAsync());
            Assert.Null(storedSourceFile.InvoiceId);
            Assert.Equal(uploadBatchFile.Id, storedSourceFile.UploadBatchFileId);
            Assert.Equal(uploadBatchFile.OriginalFileName, storedSourceFile.OriginalFileName);
            Assert.Equal(uploadBatchFile.StoredFilePath, storedSourceFile.StoredFilePath);
            Assert.Equal(preview.PreviewFilePath, storedSourceFile.PreviewFilePath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task StoreAsync_StoresMultipleRenderedPagePreviewsWithPredictableFileNames()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var uploadBatchFile = await AddUploadBatchFileAsync(dbContext);
            var firstPath = CreateRenderedPage(root, "first.png", [0x11, 0x12]);
            var secondPath = CreateRenderedPage(root, "second.png", [0x21, 0x22, 0x23, 0x24]);
            var service = CreateService(dbContext, root);

            var result = await service.StoreAsync(
                uploadBatchFile.Id,
                [
                    CreateRenderedPageResult(2, secondPath),
                    CreateRenderedPageResult(1, firstPath)
                ]);

            Assert.True(result.Succeeded);
            Assert.Collection(
                result.Previews,
                preview =>
                {
                    Assert.Equal(1, preview.PageNumber);
                    Assert.Equal(ExpectedPreviewPath(uploadBatchFile, 1), preview.PreviewFilePath);
                    Assert.Equal(2, preview.FileSizeBytes);
                },
                preview =>
                {
                    Assert.Equal(2, preview.PageNumber);
                    Assert.Equal(ExpectedPreviewPath(uploadBatchFile, 2), preview.PreviewFilePath);
                    Assert.Equal(4, preview.FileSizeBytes);
                });

            var storedSourceFiles = await dbContext.InvoiceSourceFiles
                .OrderBy(x => x.PageNumber)
                .ToListAsync();

            Assert.Collection(
                storedSourceFiles,
                sourceFile =>
                {
                    Assert.Equal(1, sourceFile.PageNumber);
                    Assert.Equal("image/png", sourceFile.ContentType);
                    Assert.Equal(2, sourceFile.FileSizeBytes);
                },
                sourceFile =>
                {
                    Assert.Equal(2, sourceFile.PageNumber);
                    Assert.Equal("image/png", sourceFile.ContentType);
                    Assert.Equal(4, sourceFile.FileSizeBytes);
                });
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task StoreAsync_CreatesPreviewDirectoryWhenItDoesNotExist()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var uploadBatchFile = await AddUploadBatchFileAsync(dbContext);
            var sourcePath = CreateRenderedPage(root, "page.png", [0x01]);
            var service = CreateService(dbContext, root);
            var previewDirectory = Path.Combine(
                root,
                "storage",
                "batches",
                uploadBatchFile.UploadBatchId.ToString("N"),
                "files",
                uploadBatchFile.Id.ToString("N"),
                "previews");

            Assert.False(Directory.Exists(previewDirectory));

            var result = await service.StoreAsync(
                uploadBatchFile.Id,
                [CreateRenderedPageResult(1, sourcePath)]);

            Assert.True(result.Succeeded);
            Assert.True(Directory.Exists(previewDirectory));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task StoreAsync_FailsClearlyIfRenderedPageFileIsMissing()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var uploadBatchFile = await AddUploadBatchFileAsync(dbContext);
            var service = CreateService(dbContext, root);

            var result = await service.StoreAsync(
                uploadBatchFile.Id,
                [CreateRenderedPageResult(1, Path.Combine(root, "missing.png"))]);

            Assert.False(result.Succeeded);
            Assert.Contains("rendered page file was not found", result.ErrorMessage);
            Assert.Empty(await dbContext.InvoiceSourceFiles.ToListAsync());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task StoreAsync_DoesNotAllowUnsafeStorageRootPaths()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var uploadBatchFile = await AddUploadBatchFileAsync(dbContext);
            var sourcePath = CreateRenderedPage(root, "page.png", [0x01]);
            var service = CreateService(dbContext, root, "../outside");

            var result = await service.StoreAsync(
                uploadBatchFile.Id,
                [CreateRenderedPageResult(1, sourcePath)]);

            Assert.False(result.Succeeded);
            Assert.Contains("storage root path is unsafe", result.ErrorMessage);
            Assert.Empty(await dbContext.InvoiceSourceFiles.ToListAsync());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static PdfPagePreviewStorageService CreateService(
        OaiDbContext dbContext,
        string basePath,
        string rootPath = "storage")
        => new(
            dbContext,
            Microsoft.Extensions.Options.Options.Create(new FileStorageOptions
            {
                BasePath = basePath,
                RootPath = rootPath,
                InvoiceFolder = "invoices"
            }),
            NullLogger<PdfPagePreviewStorageService>.Instance);

    private static OaiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OaiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new OaiDbContext(options);
    }

    private static async Task<UploadBatchFile> AddUploadBatchFileAsync(OaiDbContext dbContext)
    {
        var uploadBatch = new UploadBatch($"BATCH-{Guid.NewGuid():N}"[..33]);
        var uploadBatchFile = new UploadBatchFile(
            uploadBatch.Id,
            "invoice.pdf",
            "storage/invoices/original.pdf",
            "application/pdf",
            123);

        uploadBatch.AddFile(uploadBatchFile);
        dbContext.UploadBatches.Add(uploadBatch);
        await dbContext.SaveChangesAsync();

        return uploadBatchFile;
    }

    private static PdfRenderedPageResult CreateRenderedPageResult(
        int pageNumber,
        string imagePath)
        => new(
            pageNumber,
            imagePath,
            "image/png",
            new FileInfo(imagePath).Exists ? new FileInfo(imagePath).Length : 0,
            Width: 100,
            Height: 200);

    private static string CreateRenderedPage(
        string root,
        string fileName,
        byte[] bytes)
    {
        var directory = Path.Combine(root, "rendered");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static string ExpectedPreviewPath(UploadBatchFile uploadBatchFile, int pageNumber)
        => $"storage/batches/{uploadBatchFile.UploadBatchId:N}/files/{uploadBatchFile.Id:N}/previews/page-{pageNumber:000}.png";

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "oai-pdf-preview-storage-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
