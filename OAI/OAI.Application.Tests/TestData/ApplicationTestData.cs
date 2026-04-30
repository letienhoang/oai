using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.ValueObjects;

namespace OAI.Application.Tests.TestData;

public static class ApplicationTestData
{
    public static Vendor CreateVendor()
    {
        return new Vendor(
            name: "ACME SOFTWARE COMPANY",
            taxNumber: "TAX-001",
            address: "Ho Chi Minh City",
            email: "contact@acme.test");
    }

    public static InvoiceCreateRequestDto CreateValidInvoiceRequest(Guid vendorId)
    {
        return new InvoiceCreateRequestDto
        {
            VendorId = vendorId,
            InvoiceNumber = "INV-2026-001",
            IssueDate = new DateOnly(2026, 4, 27),
            DueDate = new DateOnly(2026, 4, 30),
            Currency = "VND",
            DeclaredSubtotal = 1500000m,
            DeclaredTaxAmount = 150000m,
            DeclaredTotalAmount = 1650000m,
            SourceFileName = "sample-invoice.png",
            SourceFilePath = "storage/invoices/sample-invoice.png",
            ExtractionEngineName = "HybridInvoiceTextParser",
            ExtractionConfidenceScore = 0.95m,
            ExtractionRawText = "ACME SOFTWARE COMPANY INV-2026-001",
            ExtractionStructuredJson = "{}",
            LineItems =
            [
                new InvoiceLineItemRequestDto
                {
                    LineNo = 1,
                    Description = "Consulting service",
                    Quantity = 1,
                    UnitPrice = 1000000m,
                    TaxRate = 10m
                },
                new InvoiceLineItemRequestDto
                {
                    LineNo = 2,
                    Description = "OCR setup",
                    Quantity = 1,
                    UnitPrice = 500000m,
                    TaxRate = 10m
                }
            ]
        };
    }

    public static Invoice CreateValidInvoice(Guid? vendorId = null)
    {
        var invoice = new Invoice(
            vendorId: vendorId ?? Guid.NewGuid(),
            invoiceNumber: "INV-2026-001",
            issueDate: new DateOnly(2026, 4, 27),
            currency: "VND",
            declaredSubtotal: new Money(1500000m, "VND"),
            declaredTaxAmount: new Money(150000m, "VND"),
            declaredTotalAmount: new Money(1650000m, "VND"),
            dueDate: new DateOnly(2026, 4, 30));

        invoice.AddLineItem(new InvoiceLineItem(
            invoice.Id,
            1,
            "Consulting service",
            1,
            new Money(1000000m, "VND"),
            10m));

        invoice.AddLineItem(new InvoiceLineItem(
            invoice.Id,
            2,
            "OCR setup",
            1,
            new Money(500000m, "VND"),
            10m));

        return invoice;
    }

    public static Invoice CreateInvalidTotalInvoice(Guid? vendorId = null)
    {
        var invoice = new Invoice(
            vendorId: vendorId ?? Guid.NewGuid(),
            invoiceNumber: "INV-INVALID-001",
            issueDate: new DateOnly(2026, 4, 27),
            currency: "VND",
            declaredSubtotal: new Money(1500000m, "VND"),
            declaredTaxAmount: new Money(150000m, "VND"),
            declaredTotalAmount: new Money(999999m, "VND"),
            dueDate: new DateOnly(2026, 4, 30));

        invoice.AddLineItem(new InvoiceLineItem(
            invoice.Id,
            1,
            "Consulting service",
            1,
            new Money(1000000m, "VND"),
            10m));

        invoice.AddLineItem(new InvoiceLineItem(
            invoice.Id,
            2,
            "OCR setup",
            1,
            new Money(500000m, "VND"),
            10m));

        return invoice;
    }
}