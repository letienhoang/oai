using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Dashboard.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.UseCases.Dashboard;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.ValueObjects;

namespace OAI.Application.Tests.UseCases.Dashboard;

public sealed class GetDashboardSummaryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldFilterOpenValidationIssueCountByVendor()
    {
        var vendorId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        var invoiceRepository = new FakeInvoiceRepository();
        var validationIssueRepository = new FakeValidationIssueRepository();
        var matchingInvoice = CreateInvoice(vendorId, "INV-MATCH");
        var otherInvoice = CreateInvoice(otherVendorId, "INV-OTHER");

        invoiceRepository.Seed(matchingInvoice);
        invoiceRepository.Seed(otherInvoice);
        validationIssueRepository.Seed(CreateIssue(matchingInvoice), matchingInvoice);
        validationIssueRepository.Seed(CreateIssue(otherInvoice), otherInvoice);

        var useCase = CreateUseCase(invoiceRepository, validationIssueRepository);

        var result = await useCase.ExecuteAsync(new GetDashboardSummaryRequestDto
        {
            Filter = new DashboardFilterDto { VendorId = vendorId }
        });

        Assert.Equal(1, result.OpenValidationIssues);
        Assert.Equal(1, result.TotalValidationIssues);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterRecentValidationIssuesByVendor()
    {
        var vendorId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        var invoiceRepository = new FakeInvoiceRepository();
        var validationIssueRepository = new FakeValidationIssueRepository();
        var matchingInvoice = CreateInvoice(vendorId, "INV-MATCH");
        var otherInvoice = CreateInvoice(otherVendorId, "INV-OTHER");

        invoiceRepository.Seed(matchingInvoice);
        invoiceRepository.Seed(otherInvoice);
        validationIssueRepository.Seed(CreateIssue(matchingInvoice), matchingInvoice);
        validationIssueRepository.Seed(CreateIssue(otherInvoice), otherInvoice);

        var useCase = CreateUseCase(invoiceRepository, validationIssueRepository);

        var result = await useCase.ExecuteAsync(new GetDashboardSummaryRequestDto
        {
            Filter = new DashboardFilterDto
            {
                VendorId = vendorId,
                RecentValidationIssueCount = 5
            }
        });

        Assert.Single(result.RecentValidationIssues);
        Assert.Equal(matchingInvoice.Id, result.RecentValidationIssues[0].InvoiceId);
    }

    private static GetDashboardSummaryUseCase CreateUseCase(
        FakeInvoiceRepository invoiceRepository,
        FakeValidationIssueRepository validationIssueRepository)
    {
        return new GetDashboardSummaryUseCase(
            invoiceRepository,
            validationIssueRepository,
            NullLogger<GetDashboardSummaryUseCase>.Instance);
    }

    private static Invoice CreateInvoice(Guid vendorId, string invoiceNumber)
    {
        return new Invoice(
            vendorId: vendorId,
            invoiceNumber: invoiceNumber,
            issueDate: new DateOnly(2026, 4, 27),
            currency: "VND",
            declaredSubtotal: new Money(1000m, "VND"),
            declaredTaxAmount: new Money(100m, "VND"),
            declaredTotalAmount: new Money(1100m, "VND"));
    }

    private static ValidationIssue CreateIssue(Invoice invoice)
    {
        return new ValidationIssue(
            invoice.Id,
            "DeclaredTotalAmount",
            "INV-012",
            "Total mismatch.",
            ValidationSeverity.Error);
    }
}
