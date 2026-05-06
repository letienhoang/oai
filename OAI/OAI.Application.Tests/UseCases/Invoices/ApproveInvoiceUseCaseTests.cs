using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.Tests.TestData;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class ApproveInvoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldApproveInvoice_WhenInvoiceHasNoUnresolvedError()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateValidInvoice();
        invoiceRepository.Seed(invoice);

        var useCase = new ApproveInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<ApproveInvoiceUseCase>.Instance);

        var result = await useCase.ExecuteAsync(new ApproveInvoiceRequestDto
        {
            InvoiceId = invoice.Id
        });

        Assert.Equal("Approved", result.Status);
        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceHasUnresolvedError()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateValidInvoice();

        invoice.AddValidationIssue(new ValidationIssue(
            invoice.Id,
            "DeclaredTotalAmount",
            "INV-012",
            "Total mismatch.",
            ValidationSeverity.Error));

        invoiceRepository.Seed(invoice);

        var useCase = new ApproveInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<ApproveInvoiceUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new ApproveInvoiceRequestDto
            {
                InvoiceId = invoice.Id
            }));

        Assert.Equal(InvoiceStatus.PendingReview, invoice.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceDoesNotExist()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new ApproveInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<ApproveInvoiceUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new ApproveInvoiceRequestDto
            {
                InvoiceId = Guid.NewGuid()
            }));
    }
}