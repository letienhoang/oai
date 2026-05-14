using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OAI.Domain.Entities;
using OAI.Domain.ValueObjects;
using OAI.Infrastructure.Files;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Tests.Files;

public sealed class InvoiceSourceFileServiceTests
{
    [Fact]
    public async Task AddOriginalSourceFileAsync_CreatesImageSourceFile()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await AddInvoiceAsync(dbContext);
        var uploadBatchFile = await AddUploadBatchFileAsync(
            dbContext,
            "invoice.png",
            "storage/invoices/invoice.png",
            "image/png",
            123);
        var service = CreateService(dbContext);

        await service.AddOriginalSourceFileAsync(invoice.Id, uploadBatchFile.Id);

        var sourceFile = Assert.Single(await dbContext.InvoiceSourceFiles.ToListAsync());
        Assert.Equal(invoice.Id, sourceFile.InvoiceId);
        Assert.Equal(uploadBatchFile.Id, sourceFile.UploadBatchFileId);
        Assert.Equal("invoice.png", sourceFile.OriginalFileName);
        Assert.Equal("storage/invoices/invoice.png", sourceFile.StoredFilePath);
        Assert.Equal("image/png", sourceFile.ContentType);
        Assert.Equal(123, sourceFile.FileSizeBytes);
        Assert.Null(sourceFile.PageNumber);
        Assert.Null(sourceFile.PreviewFilePath);
    }

    [Fact]
    public async Task AddOriginalSourceFileAsync_CreatesPdfSourceFile()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await AddInvoiceAsync(dbContext);
        var uploadBatchFile = await AddUploadBatchFileAsync(
            dbContext,
            "invoice.pdf",
            "storage/invoices/invoice.pdf",
            "application/pdf",
            456);
        var service = CreateService(dbContext);

        await service.AddOriginalSourceFileAsync(invoice.Id, uploadBatchFile.Id);

        var sourceFile = Assert.Single(await dbContext.InvoiceSourceFiles.ToListAsync());
        Assert.Equal(invoice.Id, sourceFile.InvoiceId);
        Assert.Equal("application/pdf", sourceFile.ContentType);
        Assert.Null(sourceFile.PageNumber);
    }

    [Fact]
    public async Task LinkSourceFilesToInvoiceAsync_LinksScannedPdfPreviewRecords()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await AddInvoiceAsync(dbContext);
        var uploadBatchFile = await AddUploadBatchFileAsync(
            dbContext,
            "invoice.pdf",
            "storage/invoices/invoice.pdf",
            "application/pdf",
            456);
        var preview = new InvoiceSourceFile(
            invoiceId: null,
            originalFileName: uploadBatchFile.OriginalFileName,
            storedFilePath: uploadBatchFile.StoredFilePath,
            contentType: "image/png",
            fileSizeBytes: 111,
            uploadBatchFileId: uploadBatchFile.Id,
            previewFilePath: "storage/invoices/previews/page-001.png",
            pageNumber: 1);
        await dbContext.InvoiceSourceFiles.AddAsync(preview);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.LinkSourceFilesToInvoiceAsync(invoice.Id, uploadBatchFile.Id);

        var sourceFiles = await dbContext.InvoiceSourceFiles
            .OrderBy(x => x.PageNumber ?? 0)
            .ToListAsync();

        Assert.Equal(2, sourceFiles.Count);
        Assert.All(sourceFiles, sourceFile => Assert.Equal(invoice.Id, sourceFile.InvoiceId));
        Assert.Contains(sourceFiles, sourceFile => sourceFile.PageNumber is null);
        Assert.Contains(sourceFiles, sourceFile => sourceFile.PageNumber == 1);
    }

    [Fact]
    public async Task LinkSourceFilesToInvoiceAsync_DoesNotDuplicateSourceFiles()
    {
        await using var dbContext = CreateDbContext();
        var invoice = await AddInvoiceAsync(dbContext);
        var uploadBatchFile = await AddUploadBatchFileAsync(
            dbContext,
            "invoice.pdf",
            "storage/invoices/invoice.pdf",
            "application/pdf",
            456);
        var service = CreateService(dbContext);

        await service.LinkSourceFilesToInvoiceAsync(invoice.Id, uploadBatchFile.Id);
        await service.LinkSourceFilesToInvoiceAsync(invoice.Id, uploadBatchFile.Id);

        var sourceFiles = await dbContext.InvoiceSourceFiles.ToListAsync();
        Assert.Single(sourceFiles);
        Assert.Equal(invoice.Id, sourceFiles[0].InvoiceId);
        Assert.Equal(uploadBatchFile.Id, sourceFiles[0].UploadBatchFileId);
    }

    private static InvoiceSourceFileService CreateService(OaiDbContext dbContext)
        => new(dbContext, NullLogger<InvoiceSourceFileService>.Instance);

    private static OaiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OaiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new OaiDbContext(options);
    }

    private static async Task<Invoice> AddInvoiceAsync(OaiDbContext dbContext)
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            $"INV-{Guid.NewGuid():N}",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "USD",
            new Money(1, "USD"),
            Money.Zero("USD"),
            new Money(1, "USD"));

        await dbContext.Invoices.AddAsync(invoice);
        await dbContext.SaveChangesAsync();

        return invoice;
    }

    private static async Task<UploadBatchFile> AddUploadBatchFileAsync(
        OaiDbContext dbContext,
        string fileName,
        string storedFilePath,
        string contentType,
        long fileSizeBytes)
    {
        var uploadBatch = new UploadBatch($"BATCH-{Guid.NewGuid():N}"[..33]);
        var uploadBatchFile = new UploadBatchFile(
            uploadBatch.Id,
            fileName,
            storedFilePath,
            contentType,
            fileSizeBytes);

        uploadBatch.AddFile(uploadBatchFile);
        await dbContext.UploadBatches.AddAsync(uploadBatch);
        await dbContext.SaveChangesAsync();

        return uploadBatchFile;
    }
}
