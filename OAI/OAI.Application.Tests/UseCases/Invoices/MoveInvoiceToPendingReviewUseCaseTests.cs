using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.Tests.TestData;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class MoveInvoiceToPendingReviewUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldMoveApprovedInvoiceToPendingReview()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateValidInvoice();
        invoice.Approve();

        invoiceRepository.Seed(invoice);

        var useCase = new MoveInvoiceToPendingReviewUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<MoveInvoiceToPendingReviewUseCase>.Instance);

        var result = await useCase.ExecuteAsync(new MoveInvoiceToPendingReviewRequestDto
        {
            InvoiceId = invoice.Id
        });

        Assert.Equal("PendingReview", result.Status);
        Assert.Equal(InvoiceStatus.PendingReview, invoice.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceDoesNotExist()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new MoveInvoiceToPendingReviewUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<MoveInvoiceToPendingReviewUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new MoveInvoiceToPendingReviewRequestDto
            {
                InvoiceId = Guid.NewGuid()
            }));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceIsExported()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateValidInvoice();
        invoice.MarkExported();

        invoiceRepository.Seed(invoice);

        var useCase = new MoveInvoiceToPendingReviewUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<MoveInvoiceToPendingReviewUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new MoveInvoiceToPendingReviewRequestDto
            {
                InvoiceId = invoice.Id
            }));

        Assert.Equal(InvoiceStatus.Exported, invoice.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}