using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IReportExportService
{
    Task<byte[]> ExportInvoicesToExcelAsync(
        IReadOnlyList<InvoiceDetailDto> invoices,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportInvoiceToPdfAsync(
        InvoiceDetailDto invoice,
        CancellationToken cancellationToken = default);
}