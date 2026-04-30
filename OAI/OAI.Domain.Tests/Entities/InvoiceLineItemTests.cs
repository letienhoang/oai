using OAI.Domain.Entities;
using OAI.Domain.ValueObjects;

namespace OAI.Domain.Tests.Entities;

public sealed class InvoiceLineItemTests
{
    [Fact]
    public void Constructor_ShouldCreateValidLineItem()
    {
        var invoiceId = Guid.NewGuid();

        var item = new InvoiceLineItem(
            invoiceId,
            lineNo: 1,
            description: "Consulting service",
            quantity: 2,
            unitPrice: new Money(100000m, "VND"),
            taxRate: 10m);

        Assert.Equal(invoiceId, item.InvoiceId);
        Assert.Equal(1, item.LineNo);
        Assert.Equal("Consulting service", item.Description);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(100000m, item.UnitPrice.Amount);
        Assert.Equal(10m, item.TaxRate);
    }

    [Fact]
    public void NetAmount_ShouldEqualQuantityMultiplyUnitPrice()
    {
        var item = new InvoiceLineItem(
            Guid.NewGuid(),
            1,
            "OCR setup",
            2,
            new Money(500000m, "VND"),
            10m);

        Assert.Equal(1000000m, item.NetAmount.Amount);
    }

    [Fact]
    public void TaxAmount_ShouldEqualNetAmountMultiplyTaxRate()
    {
        var item = new InvoiceLineItem(
            Guid.NewGuid(),
            1,
            "OCR setup",
            2,
            new Money(500000m, "VND"),
            10m);

        Assert.Equal(100000m, item.TaxAmount.Amount);
    }

    [Fact]
    public void GrossAmount_ShouldEqualNetAmountPlusTaxAmount()
    {
        var item = new InvoiceLineItem(
            Guid.NewGuid(),
            1,
            "OCR setup",
            2,
            new Money(500000m, "VND"),
            10m);

        Assert.Equal(1100000m, item.GrossAmount.Amount);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenQuantityIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new InvoiceLineItem(
                Guid.NewGuid(),
                1,
                "Invalid item",
                0,
                new Money(100000m, "VND"),
                10m));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTaxRateIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new InvoiceLineItem(
                Guid.NewGuid(),
                1,
                "Invalid item",
                1,
                new Money(100000m, "VND"),
                -1m));
    }
}