using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IValidateInvoiceUseCase
{
    Task<InvoiceValidationResultDto> ExecuteAsync(
        ValidateInvoiceRequestDto request,
        CancellationToken cancellationToken = default);
}