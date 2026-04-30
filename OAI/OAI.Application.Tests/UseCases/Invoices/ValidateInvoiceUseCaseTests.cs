using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.Tests.TestData;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Exceptions;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class ValidateInvoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidResult_WhenInvoiceIsConsistent()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateValidInvoice();
        invoiceRepository.Seed(invoice);

        var useCase = new ValidateInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<ValidateInvoiceUseCase>.Instance);

        var result = await useCase.ExecuteAsync(new ValidateInvoiceRequestDto
        {
            InvoiceId = invoice.Id
        });

        Assert.True(result.IsValid);
        Assert.Equal(0, result.ErrorCount);
        Assert.Empty(result.Issues);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnInvalidResult_WhenInvoiceHasWrongTotal()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var invoice = ApplicationTestData.CreateInvalidTotalInvoice();
        invoiceRepository.Seed(invoice);

        var useCase = new ValidateInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<ValidateInvoiceUseCase>.Instance);

        var result = await useCase.ExecuteAsync(new ValidateInvoiceRequestDto
        {
            InvoiceId = invoice.Id
        });

        Assert.False(result.IsValid);
        Assert.True(result.ErrorCount > 0);
        Assert.Contains(result.Issues, x => x.RuleCode == "INV-012");
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceDoesNotExist()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new ValidateInvoiceUseCase(
            invoiceRepository,
            unitOfWork,
            NullLogger<ValidateInvoiceUseCase>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new ValidateInvoiceRequestDto
            {
                InvoiceId = Guid.NewGuid()
            }));
    }
}