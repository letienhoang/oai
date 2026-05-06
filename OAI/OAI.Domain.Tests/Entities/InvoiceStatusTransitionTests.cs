using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;
using OAI.Domain.ValueObjects;

namespace OAI.Domain.Tests.Entities;

public sealed class InvoiceStatusTransitionTests
{
    [Fact]
    public void Constructor_ShouldSetStatusToPendingReview()
    {
        var invoice = CreateValidInvoice();

        Assert.Equal(InvoiceStatus.PendingReview, invoice.Status);
    }

    [Fact]
    public void Approve_ShouldSetStatusToApproved_WhenNoUnresolvedErrorExists()
    {
        var invoice = CreateValidInvoice();

        invoice.Approve();

        Assert.Equal(InvoiceStatus.Approved, invoice.Status);
    }

    [Fact]
    public void Approve_ShouldThrow_WhenInvoiceHasUnresolvedError()
    {
        var invoice = CreateValidInvoice();

        invoice.AddValidationIssue(new ValidationIssue(
            invoice.Id,
            fieldName: "DeclaredTotalAmount",
            ruleCode: "INV-012",
            message: "Total amount mismatch.",
            severity: ValidationSeverity.Error));

        Assert.Throws<DomainException>(() => invoice.Approve());
    }

    [Fact]
    public void Reject_ShouldSetStatusToRejected()
    {
        var invoice = CreateValidInvoice();

        invoice.Reject();

        Assert.Equal(InvoiceStatus.Rejected, invoice.Status);
    }

    [Fact]
    public void MoveToPendingReview_ShouldSetStatusToPendingReview_WhenInvoiceIsApproved()
    {
        var invoice = CreateValidInvoice();

        invoice.Approve();
        invoice.MoveToPendingReview();

        Assert.Equal(InvoiceStatus.PendingReview, invoice.Status);
    }

    [Fact]
    public void Reject_ShouldThrow_WhenInvoiceIsExported()
    {
        var invoice = CreateValidInvoice();

        invoice.MarkExported();

        Assert.Throws<DomainException>(() => invoice.Reject());
    }

    [Fact]
    public void MoveToPendingReview_ShouldThrow_WhenInvoiceIsExported()
    {
        var invoice = CreateValidInvoice();

        invoice.MarkExported();

        Assert.Throws<DomainException>(() => invoice.MoveToPendingReview());
    }

    private static Invoice CreateValidInvoice()
    {
        var invoice = new Invoice(
            vendorId: Guid.NewGuid(),
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
}