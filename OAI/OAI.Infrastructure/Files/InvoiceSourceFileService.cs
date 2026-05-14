using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OAI.Application.Files;
using OAI.Domain.Entities;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Files;

public sealed class InvoiceSourceFileService : IInvoiceSourceFileService
{
    private readonly OaiDbContext _dbContext;
    private readonly ILogger<InvoiceSourceFileService> _logger;

    public InvoiceSourceFileService(
        OaiDbContext dbContext,
        ILogger<InvoiceSourceFileService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task AddOriginalSourceFileAsync(
        Guid invoiceId,
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        if (uploadBatchFileId == Guid.Empty)
            throw new ArgumentException("UploadBatchFileId cannot be empty.", nameof(uploadBatchFileId));

        var uploadBatchFile = await _dbContext.UploadBatchFiles
            .FirstOrDefaultAsync(x => x.Id == uploadBatchFileId, cancellationToken);

        if (uploadBatchFile is null)
        {
            throw new InvalidOperationException(
                $"Upload batch file '{uploadBatchFileId}' was not found.");
        }

        var existingOriginal = await _dbContext.InvoiceSourceFiles
            .FirstOrDefaultAsync(
                x => x.UploadBatchFileId == uploadBatchFileId &&
                     x.PageNumber == null &&
                     x.PreviewFilePath == null,
                cancellationToken);

        if (existingOriginal is not null)
        {
            if (existingOriginal.InvoiceId is null)
            {
                existingOriginal.LinkInvoice(invoiceId);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var sourceFile = new InvoiceSourceFile(
            invoiceId,
            uploadBatchFile.OriginalFileName,
            uploadBatchFile.StoredFilePath,
            uploadBatchFile.ContentType,
            uploadBatchFile.FileSizeBytes,
            uploadBatchFile.Id);

        await _dbContext.InvoiceSourceFiles.AddAsync(sourceFile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Original invoice source file metadata created. InvoiceId: {InvoiceId}, UploadBatchFileId: {UploadBatchFileId}, SourceFileId: {SourceFileId}",
            invoiceId,
            uploadBatchFileId,
            sourceFile.Id);
    }

    public async Task LinkSourceFilesToInvoiceAsync(
        Guid invoiceId,
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        if (uploadBatchFileId == Guid.Empty)
            throw new ArgumentException("UploadBatchFileId cannot be empty.", nameof(uploadBatchFileId));

        await AddOriginalSourceFileAsync(invoiceId, uploadBatchFileId, cancellationToken);

        var unlinkedSourceFiles = await _dbContext.InvoiceSourceFiles
            .Where(x => x.UploadBatchFileId == uploadBatchFileId &&
                        x.InvoiceId == null)
            .OrderBy(x => x.PageNumber ?? int.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var sourceFile in unlinkedSourceFiles)
        {
            sourceFile.LinkInvoice(invoiceId);
        }

        if (unlinkedSourceFiles.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
