using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IMoveInvoiceToPendingReviewUseCase
{
    Task<MoveInvoiceToPendingReviewResultDto> ExecuteAsync(
        MoveInvoiceToPendingReviewRequestDto request,
        CancellationToken cancellationToken = default);
}