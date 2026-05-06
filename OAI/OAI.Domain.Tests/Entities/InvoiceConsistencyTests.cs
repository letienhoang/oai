using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.ValueObjects;

namespace OAI.Domain.Tests.Entities;

public sealed class InvoiceConsistencyTests
{
    [Fact]
    public void ValidateConsistency_ShouldReturnNoIssue_WhenInvoiceIsValid()
    {
        var invoice = CreateInvoice(
            subtotal: 1500000m,
            tax: 150000m,
            total: 1650000m);

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

        var issues = invoice.ValidateConsistency();

        Assert.Empty(issues);
    }

    [Fact]
    public void ValidateConsistency_ShouldReturnError_WhenInvoiceHasNoLineItem()
    {
        var invoice = CreateInvoice(
            subtotal: 0m,
            tax: 0m,
            total: 0m);

        var issues = invoice.ValidateConsistency();

        Assert.Contains(issues, x =>
            x.RuleCode == "INV-001" &&
            x.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void ValidateConsistency_ShouldReturnError_WhenSubtotalMismatch()
    {
        var invoice = CreateInvoice(
            subtotal: 999999m,
            tax: 150000m,
            total: 1650000m);

        AddDefaultLineItems(invoice);

        var issues = invoice.ValidateConsistency();

        Assert.Contains(issues, x => x.RuleCode == "INV-010");
    }

    [Fact]
    public void ValidateConsistency_ShouldReturnError_WhenTaxAmountMismatch()
    {
        var invoice = CreateInvoice(
            subtotal: 1500000m,
            tax: 999999m,
            total: 1650000m);

        AddDefaultLineItems(invoice);

        var issues = invoice.ValidateConsistency();

        Assert.Contains(issues, x => x.RuleCode == "INV-011");
    }

    [Fact]
    public void ValidateConsistency_ShouldReturnError_WhenTotalAmountMismatch()
    {
        var invoice = CreateInvoice(
            subtotal: 1500000m,
            tax: 150000m,
            total: 999999m);

        AddDefaultLineItems(invoice);

        var issues = invoice.ValidateConsistency();

        Assert.Contains(issues, x => x.RuleCode == "INV-012");
    }

    private static Invoice CreateInvoice(decimal subtotal, decimal tax, decimal total)
    {
        return new Invoice(
            vendorId: Guid.NewGuid(),
            invoiceNumber: "INV-2026-001",
            issueDate: new DateOnly(2026, 4, 27),
            currency: "VND",
            declaredSubtotal: new Money(subtotal, "VND"),
            declaredTaxAmount: new Money(tax, "VND"),
            declaredTotalAmount: new Money(total, "VND"),
            dueDate: new DateOnly(2026, 4, 30));
    }

    private static void AddDefaultLineItems(Invoice invoice)
    {
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
    }
}