using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.ValueObjects;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class GetInvoiceListUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldFilterByStatus()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var approvedInvoice = CreateInvoice("INV-APPROVED", InvoiceStatus.Approved);
        var pendingInvoice = CreateInvoice("INV-PENDING", InvoiceStatus.PendingReview);

        invoiceRepository.Seed(approvedInvoice);
        invoiceRepository.Seed(pendingInvoice);

        var useCase = CreateUseCase(invoiceRepository);

        var result = await useCase.ExecuteAsync(new GetInvoiceListRequestDto
        {
            Filter = new InvoiceListFilterDto { Status = InvoiceStatus.Approved }
        });

        Assert.Single(result.Items);
        Assert.Equal(approvedInvoice.Id, result.Items[0].InvoiceId);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterByIssueDateRange()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var insideRange = CreateInvoice("INV-IN-RANGE", issueDate: new DateOnly(2026, 4, 15));
        var beforeRange = CreateInvoice("INV-BEFORE", issueDate: new DateOnly(2026, 3, 31));
        var afterRange = CreateInvoice("INV-AFTER", issueDate: new DateOnly(2026, 5, 1));

        invoiceRepository.Seed(insideRange);
        invoiceRepository.Seed(beforeRange);
        invoiceRepository.Seed(afterRange);

        var useCase = CreateUseCase(invoiceRepository);

        var result = await useCase.ExecuteAsync(new GetInvoiceListRequestDto
        {
            Filter = new InvoiceListFilterDto
            {
                IssueDateFrom = new DateOnly(2026, 4, 1),
                IssueDateTo = new DateOnly(2026, 4, 30)
            }
        });

        Assert.Single(result.Items);
        Assert.Equal(insideRange.Id, result.Items[0].InvoiceId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterByHasOpenValidationIssues()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var invoiceWithOpenIssue = CreateInvoice("INV-OPEN-ISSUE");
        var invoiceWithResolvedIssue = CreateInvoice("INV-RESOLVED-ISSUE");
        var resolvedIssue = CreateIssue(invoiceWithResolvedIssue);

        resolvedIssue.Resolve();
        invoiceWithOpenIssue.AddValidationIssue(CreateIssue(invoiceWithOpenIssue));
        invoiceWithResolvedIssue.AddValidationIssue(resolvedIssue);

        invoiceRepository.Seed(invoiceWithOpenIssue);
        invoiceRepository.Seed(invoiceWithResolvedIssue);
        invoiceRepository.Seed(CreateInvoice("INV-NO-ISSUE"));

        var useCase = CreateUseCase(invoiceRepository);

        var result = await useCase.ExecuteAsync(new GetInvoiceListRequestDto
        {
            Filter = new InvoiceListFilterDto { HasOpenValidationIssues = true }
        });

        Assert.Single(result.Items);
        Assert.Equal(invoiceWithOpenIssue.Id, result.Items[0].InvoiceId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCapPageSizeAt100()
    {
        var invoiceRepository = new FakeInvoiceRepository();

        for (var i = 0; i < 101; i++)
            invoiceRepository.Seed(CreateInvoice($"INV-{i:000}"));

        var useCase = CreateUseCase(invoiceRepository);

        var result = await useCase.ExecuteAsync(new GetInvoiceListRequestDto
        {
            PageSize = 101
        });

        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
        Assert.Equal(101, result.TotalItems);
    }

    private static GetInvoiceListUseCase CreateUseCase(FakeInvoiceRepository invoiceRepository)
    {
        return new GetInvoiceListUseCase(
            invoiceRepository,
            NullLogger<GetInvoiceListUseCase>.Instance);
    }

    private static Invoice CreateInvoice(
        string invoiceNumber,
        InvoiceStatus status = InvoiceStatus.PendingReview,
        DateOnly? issueDate = null)
    {
        var invoice = new Invoice(
            vendorId: Guid.NewGuid(),
            invoiceNumber: invoiceNumber,
            issueDate: issueDate ?? new DateOnly(2026, 4, 27),
            currency: "VND",
            declaredSubtotal: new Money(1000m, "VND"),
            declaredTaxAmount: new Money(100m, "VND"),
            declaredTotalAmount: new Money(1100m, "VND"));

        if (status == InvoiceStatus.Approved)
            invoice.Approve();
        else if (status == InvoiceStatus.Rejected)
            invoice.Reject();
        else if (status == InvoiceStatus.Exported)
            invoice.MarkExported();

        return invoice;
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
