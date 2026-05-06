using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Tests.Fakes;
using OAI.Application.Tests.TestData;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Exceptions;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class CreateInvoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCreateInvoice_WhenRequestIsValid()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var vendorRepository = new FakeVendorRepository();
        var unitOfWork = new FakeUnitOfWork();

        var vendor = ApplicationTestData.CreateVendor();
        vendorRepository.Seed(vendor);

        var useCase = new CreateInvoiceUseCase(
            invoiceRepository,
            vendorRepository,
            unitOfWork,
            NullLogger<CreateInvoiceUseCase>.Instance);

        var request = ApplicationTestData.CreateValidInvoiceRequest(vendor.Id);

        var result = await useCase.ExecuteAsync(request);

        Assert.Equal("INV-2026-001", result.InvoiceNumber);
        Assert.Single(invoiceRepository.Invoices);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenVendorDoesNotExist()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var vendorRepository = new FakeVendorRepository();
        var unitOfWork = new FakeUnitOfWork();

        var useCase = new CreateInvoiceUseCase(
            invoiceRepository,
            vendorRepository,
            unitOfWork,
            NullLogger<CreateInvoiceUseCase>.Instance);

        var request = ApplicationTestData.CreateValidInvoiceRequest(Guid.NewGuid());

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenInvoiceNumberAlreadyExists()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var vendorRepository = new FakeVendorRepository();
        var unitOfWork = new FakeUnitOfWork();

        var vendor = ApplicationTestData.CreateVendor();
        vendorRepository.Seed(vendor);

        var existingInvoice = ApplicationTestData.CreateValidInvoice(vendor.Id);
        invoiceRepository.Seed(existingInvoice);

        var useCase = new CreateInvoiceUseCase(
            invoiceRepository,
            vendorRepository,
            unitOfWork,
            NullLogger<CreateInvoiceUseCase>.Instance);

        var request = ApplicationTestData.CreateValidInvoiceRequest(vendor.Id);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateValidationIssues_WhenTotalsAreInconsistent()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var vendorRepository = new FakeVendorRepository();
        var unitOfWork = new FakeUnitOfWork();

        var vendor = ApplicationTestData.CreateVendor();
        vendorRepository.Seed(vendor);

        var useCase = new CreateInvoiceUseCase(
            invoiceRepository,
            vendorRepository,
            unitOfWork,
            NullLogger<CreateInvoiceUseCase>.Instance);

        var request = ApplicationTestData.CreateValidInvoiceRequest(vendor.Id) with
        {
            DeclaredTotalAmount = 999999m
        };

        var result = await useCase.ExecuteAsync(request);

        var savedInvoice = invoiceRepository.Invoices.Single();

        Assert.NotEmpty(savedInvoice.ValidationIssues);
        Assert.Contains(savedInvoice.ValidationIssues, x => x.RuleCode == "INV-012");
        Assert.Equal(result.InvoiceId, savedInvoice.Id);
    }
}