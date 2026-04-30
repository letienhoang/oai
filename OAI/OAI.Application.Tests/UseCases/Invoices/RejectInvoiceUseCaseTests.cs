using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.Tests.TestData;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class RejectInvoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRejectInvoice()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateValidInvoice();
        invoiceRepository.Seed(invoice);

        var useCase = new RejectInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<RejectInvoiceUseCase>.Instance);

        var result = await useCase.ExecuteAsync(new RejectInvoiceRequestDto
        {
            InvoiceId = invoice.Id
        });

        Assert.Equal("Rejected", result.Status);
        Assert.Equal(InvoiceStatus.Rejected, invoice.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceDoesNotExist()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new RejectInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<RejectInvoiceUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new RejectInvoiceRequestDto
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

        var useCase = new RejectInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<RejectInvoiceUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new RejectInvoiceRequestDto
            {
                InvoiceId = invoice.Id
            }));

        Assert.Equal(InvoiceStatus.Exported, invoice.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}