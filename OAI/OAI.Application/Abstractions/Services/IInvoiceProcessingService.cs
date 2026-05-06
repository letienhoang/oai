using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceProcessingService
{
    Task<InvoiceUploadResultDto> UploadInvoiceAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<InvoiceDetailDto?> ProcessInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<InvoiceDetailDto?> ReprocessInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}