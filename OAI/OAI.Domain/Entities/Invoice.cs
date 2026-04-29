using OAI.Domain.Common;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;
using OAI.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OAI.Domain.Entities;

public sealed class Invoice : Entity
{
    private readonly List<InvoiceLineItem> _lineItems = new();
    private readonly List<ValidationIssue> _validationIssues = new();
    private readonly List<InvoiceExtractionResult> _extractionResults = new();

    public Guid VendorId { get; private set; }
    public Vendor? Vendor { get; private set; }

    public string InvoiceNumber { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public string Currency { get; private set; }

    public Money DeclaredSubtotal { get; private set; }
    public Money DeclaredTaxAmount { get; private set; }
    public Money DeclaredTotalAmount { get; private set; }

    public InvoiceStatus Status { get; private set; }
    public string? SourceFileName { get; private set; }
    public string? SourceFilePath { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();
    public IReadOnlyCollection<ValidationIssue> ValidationIssues => _validationIssues.AsReadOnly();
    public IReadOnlyCollection<InvoiceExtractionResult> ExtractionResults => _extractionResults.AsReadOnly();

    private Invoice()
    {
        InvoiceNumber = string.Empty;
        Currency = "VND";
        DeclaredSubtotal = Money.Zero("VND");
        DeclaredTaxAmount = Money.Zero("VND");
        DeclaredTotalAmount = Money.Zero("VND");
        Status = InvoiceStatus.Draft;
    }

    public Invoice(
        Guid vendorId,
        string invoiceNumber,
        DateOnly issueDate,
        string currency,
        Money declaredSubtotal,
        Money declaredTaxAmount,
        Money declaredTotalAmount,
        DateOnly? dueDate = null,
        string? sourceFileName = null,
        string? sourceFilePath = null)
    {
        if (vendorId == Guid.Empty)
            throw new ArgumentException("VendorId cannot be empty.", nameof(vendorId));

        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        VendorId = vendorId;
        InvoiceNumber = invoiceNumber.Trim();
        IssueDate = issueDate;
        DueDate = dueDate;
        Currency = currency.Trim().ToUpperInvariant();

        DeclaredSubtotal = declaredSubtotal ?? throw new ArgumentNullException(nameof(declaredSubtotal));
        DeclaredTaxAmount = declaredTaxAmount ?? throw new ArgumentNullException(nameof(declaredTaxAmount));
        DeclaredTotalAmount = declaredTotalAmount ?? throw new ArgumentNullException(nameof(declaredTotalAmount));

        EnsureCurrencyMatch(DeclaredSubtotal);
        EnsureCurrencyMatch(DeclaredTaxAmount);
        EnsureCurrencyMatch(DeclaredTotalAmount);

        SourceFileName = string.IsNullOrWhiteSpace(sourceFileName) ? null : sourceFileName.Trim();
        SourceFilePath = string.IsNullOrWhiteSpace(sourceFilePath) ? null : sourceFilePath.Trim();
        Status = InvoiceStatus.PendingReview;
    }

    public void AssignVendor(Vendor vendor)
    {
        Vendor = vendor ?? throw new ArgumentNullException(nameof(vendor));
        VendorId = vendor.Id;
        Touch();
    }

    public void UpdateDeclaredAmounts(Money subtotal, Money taxAmount, Money totalAmount)
    {
        DeclaredSubtotal = subtotal ?? throw new ArgumentNullException(nameof(subtotal));
        DeclaredTaxAmount = taxAmount ?? throw new ArgumentNullException(nameof(taxAmount));
        DeclaredTotalAmount = totalAmount ?? throw new ArgumentNullException(nameof(totalAmount));

        EnsureCurrencyMatch(DeclaredSubtotal);
        EnsureCurrencyMatch(DeclaredTaxAmount);
        EnsureCurrencyMatch(DeclaredTotalAmount);

        Touch();
    }

    public void AddLineItem(InvoiceLineItem item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (item.InvoiceId != Id)
            throw new DomainException("Line item does not belong to this invoice.");

        _lineItems.Add(item);
        Touch();
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        var lineItem = _lineItems.FirstOrDefault(x => x.Id == lineItemId);
        if (lineItem is null)
            return;

        _lineItems.Remove(lineItem);
        Touch();
    }

    public void AddValidationIssue(ValidationIssue issue)
    {
        if (issue is null)
            throw new ArgumentNullException(nameof(issue));

        if (issue.InvoiceId != Id)
            throw new DomainException("Validation issue does not belong to this invoice.");

        _validationIssues.Add(issue);
        Touch();
    }
    
    public void ReplaceValidationIssues(IEnumerable<ValidationIssue> issues)
    {
        _validationIssues.Clear();

        foreach (var issue in issues)
        {
            if (issue.InvoiceId != Id)
                throw new DomainException("Validation issue does not belong to this invoice.");

            _validationIssues.Add(issue);
        }

        Touch();
    }

    public void AddExtractionResult(InvoiceExtractionResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        if (result.InvoiceId != Id)
            throw new DomainException("Extraction result does not belong to this invoice.");

        _extractionResults.Add(result);
        Touch();
    }

    public Money CalculateSubtotal()
    {
        var total = Money.Zero(Currency);

        foreach (var item in _lineItems)
            total += item.NetAmount;

        return total;
    }

    public Money CalculateTaxAmount()
    {
        var total = Money.Zero(Currency);

        foreach (var item in _lineItems)
            total += item.TaxAmount;

        return total;
    }

    public Money CalculateGrandTotal()
    {
        var subtotal = CalculateSubtotal();
        var tax = CalculateTaxAmount();
        return subtotal + tax;
    }

    public IReadOnlyCollection<ValidationIssue> ValidateConsistency(decimal tolerance = 0.01m)
    {
        var issues = new List<ValidationIssue>();

        if (_lineItems.Count == 0)
        {
            issues.Add(new ValidationIssue(
                Id,
                fieldName: "LineItems",
                ruleCode: "INV-001",
                message: "Invoice must contain at least one line item.",
                severity: ValidationSeverity.Error));
        }

        var calculatedSubtotal = CalculateSubtotal();
        var calculatedTax = CalculateTaxAmount();
        var calculatedTotal = CalculateGrandTotal();

        if (!DeclaredSubtotal.IsCloseTo(calculatedSubtotal, tolerance))
        {
            issues.Add(new ValidationIssue(
                Id,
                fieldName: nameof(DeclaredSubtotal),
                ruleCode: "INV-010",
                message: $"Declared subtotal ({DeclaredSubtotal}) does not match calculated subtotal ({calculatedSubtotal}).",
                severity: ValidationSeverity.Error));
        }

        if (!DeclaredTaxAmount.IsCloseTo(calculatedTax, tolerance))
        {
            issues.Add(new ValidationIssue(
                Id,
                fieldName: nameof(DeclaredTaxAmount),
                ruleCode: "INV-011",
                message: $"Declared tax amount ({DeclaredTaxAmount}) does not match calculated tax amount ({calculatedTax}).",
                severity: ValidationSeverity.Error));
        }

        if (!DeclaredTotalAmount.IsCloseTo(calculatedTotal, tolerance))
        {
            issues.Add(new ValidationIssue(
                Id,
                fieldName: nameof(DeclaredTotalAmount),
                ruleCode: "INV-012",
                message: $"Declared total amount ({DeclaredTotalAmount}) does not match calculated total ({calculatedTotal}).",
                severity: ValidationSeverity.Error));
        }

        return issues;
    }

    public void Approve()
    {
        if (Status == InvoiceStatus.Approved)
            return;

        if (Status == InvoiceStatus.Exported)
            throw new DomainException("Exported invoice cannot be approved again.");

        var hasOpenError = ValidationIssues.Any(x =>
            x.Severity == ValidationSeverity.Error &&
            !x.IsResolved);

        if (hasOpenError)
            throw new DomainException("Invoice cannot be approved because it still has unresolved validation errors.");

        Status = InvoiceStatus.Approved;
        Touch();
    }

    public void Reject()
    {
        Status = InvoiceStatus.Rejected;
        Touch();
    }

    public void MarkExported()
    {
        Status = InvoiceStatus.Exported;
        Touch();
    }
    
    public void UpdateHeader(
        Guid vendorId,
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly? dueDate,
        string currency)
    {
        if (vendorId == Guid.Empty)
            throw new ArgumentException("VendorId cannot be empty.", nameof(vendorId));

        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        VendorId = vendorId;
        InvoiceNumber = invoiceNumber.Trim();
        IssueDate = issueDate;
        DueDate = dueDate;
        Currency = currency.Trim().ToUpperInvariant();

        Touch();
    }
    
    public void ReplaceLineItems(IEnumerable<InvoiceLineItem> lineItems)
    {
        _lineItems.Clear();

        foreach (var item in lineItems)
        {
            if (item.InvoiceId != Id)
                throw new DomainException("Line item does not belong to this invoice.");

            _lineItems.Add(item);
        }

        Touch();
    }
    
    public void MarkAsPendingReview()
    {
        if (Status == InvoiceStatus.Exported)
            throw new DomainException("Exported invoice cannot be moved back to pending review.");

        Status = InvoiceStatus.PendingReview;
        Touch();
    }

    private void EnsureCurrencyMatch(Money money)
    {
        if (!string.Equals(Currency, money.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Invoice currency does not match money currency.");
    }
}
