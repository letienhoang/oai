using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Tests.Fakes;
using OAI.Application.UseCases.Invoices;
using OAI.Domain.Entities;
using OAI.Domain.ValueObjects;

namespace OAI.Application.Tests.UseCases.Invoices;

public sealed class GetInvoiceDetailUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_IncludesSafeSourceFileMetadata()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var invoice = CreateInvoice();
        var sourceFile = new InvoiceSourceFile(
            invoice.Id,
            "invoice.pdf",
            "storage/invoices/invoice.pdf",
            "application/pdf",
            2048);

        invoice.AddSourceFile(sourceFile);
        invoiceRepository.Seed(invoice);

        var result = await CreateUseCase(invoiceRepository).ExecuteAsync(new GetInvoiceDetailRequestDto
        {
            InvoiceId = invoice.Id
        });

        var resultSourceFile = Assert.Single(result.SourceFiles);

        Assert.Equal(sourceFile.Id, resultSourceFile.Id);
        Assert.Equal("invoice.pdf", resultSourceFile.OriginalFileName);
        Assert.Equal("application/pdf", resultSourceFile.ContentType);
        Assert.Equal(2048, resultSourceFile.FileSizeBytes);
        Assert.Null(resultSourceFile.PageNumber);
        Assert.Equal(sourceFile.CreatedAt, resultSourceFile.CreatedAt);
        Assert.DoesNotContain(
            typeof(InvoiceSourceFileDto).GetProperties(),
            property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_OrdersSourceFilesWithOriginalFileFirstThenPageNumber()
    {
        var invoiceRepository = new FakeInvoiceRepository();
        var invoice = CreateInvoice();
        var originalFile = new InvoiceSourceFile(
            invoice.Id,
            "invoice.pdf",
            "storage/invoices/invoice.pdf",
            "application/pdf",
            4096);
        var pageTwo = new InvoiceSourceFile(
            invoice.Id,
            "invoice.pdf",
            "storage/invoices/previews/page-002.png",
            "image/png",
            200,
            previewFilePath: "storage/invoices/previews/page-002.png",
            pageNumber: 2);
        var pageOne = new InvoiceSourceFile(
            invoice.Id,
            "invoice.pdf",
            "storage/invoices/previews/page-001.png",
            "image/png",
            100,
            previewFilePath: "storage/invoices/previews/page-001.png",
            pageNumber: 1);

        invoice.AddSourceFile(originalFile);
        invoice.AddSourceFile(pageTwo);
        invoice.AddSourceFile(pageOne);
        invoiceRepository.Seed(invoice);

        var result = await CreateUseCase(invoiceRepository).ExecuteAsync(new GetInvoiceDetailRequestDto
        {
            InvoiceId = invoice.Id
        });

        Assert.Equal([null, 1, 2], result.SourceFiles.Select(x => x.PageNumber));
    }

    private static GetInvoiceDetailUseCase CreateUseCase(FakeInvoiceRepository invoiceRepository)
    {
        return new GetInvoiceDetailUseCase(
            invoiceRepository,
            NullLogger<GetInvoiceDetailUseCase>.Instance);
    }

    private static Invoice CreateInvoice()
    {
        return new Invoice(
            vendorId: Guid.NewGuid(),
            invoiceNumber: $"INV-{Guid.NewGuid():N}",
            issueDate: new DateOnly(2026, 5, 14),
            currency: "VND",
            declaredSubtotal: new Money(1000m, "VND"),
            declaredTaxAmount: new Money(100m, "VND"),
            declaredTotalAmount: new Money(1100m, "VND"));
    }
}
