using OAI.Domain.Common;
using OAI.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Entities;

public sealed class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; private set; }
    public int LineNo { get; private set; }
    public string Description { get; private set; }
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; } // ex: 10 = 10%

    public Money NetAmount => new(Quantity * UnitPrice.Amount, UnitPrice.Currency);
    public Money TaxAmount => new(NetAmount.Amount * TaxRate / 100m, UnitPrice.Currency);
    public Money GrossAmount => new(NetAmount.Amount + TaxAmount.Amount, UnitPrice.Currency);

    private InvoiceLineItem()
    {
        Description = string.Empty;
        UnitPrice = new Money(0m, "VND");
    }

    public InvoiceLineItem(
        Guid invoiceId,
        int lineNo,
        string description,
        decimal quantity,
        Money unitPrice,
        decimal taxRate = 0m)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        if (lineNo <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineNo), "Line number must be greater than zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        if (taxRate < 0)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate cannot be negative.");

        InvoiceId = invoiceId;
        LineNo = lineNo;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        TaxRate = taxRate;
    }

    public void UpdateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        Quantity = quantity;
        Touch();
    }

    public void UpdatePricing(Money unitPrice, decimal taxRate)
    {
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));

        if (taxRate < 0)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate cannot be negative.");

        TaxRate = taxRate;
        Touch();
    }
}
