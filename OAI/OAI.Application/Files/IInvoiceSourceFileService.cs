namespace OAI.Application.Files;

public interface IInvoiceSourceFileService
{
    Task AddOriginalSourceFileAsync(
        Guid invoiceId,
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default);

    Task LinkSourceFilesToInvoiceAsync(
        Guid invoiceId,
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default);
}
