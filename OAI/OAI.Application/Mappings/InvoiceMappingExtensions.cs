using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;

namespace OAI.Application.Mappings;

public static class InvoiceMappingExtensions
{
    public static InvoiceDetailDto ToDetailDto(this Invoice invoice)
    {
        return new InvoiceDetailDto
        {
            InvoiceId = invoice.Id,
            VendorId = invoice.VendorId,
            VendorName = invoice.Vendor?.Name ?? string.Empty,
            InvoiceNumber = invoice.InvoiceNumber,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Currency = invoice.Currency,
            DeclaredSubtotal = invoice.DeclaredSubtotal.Amount,
            DeclaredTaxAmount = invoice.DeclaredTaxAmount.Amount,
            DeclaredTotalAmount = invoice.DeclaredTotalAmount.Amount,
            Status = invoice.Status.ToString(),
            SourceFileName = invoice.SourceFileName,
            LineItems = invoice.LineItems
                .OrderBy(x => x.LineNo)
                .Select(x => x.ToDto())
                .ToList(),
            ValidationIssues = invoice.ValidationIssues
                .OrderBy(x => x.DetectedAt)
                .Select(x => x.ToDto())
                .ToList(),
            ExtractionResults = invoice.ExtractionResults
                .OrderByDescending(x => x.ExtractedAt)
                .Select(x => x.ToDto())
                .ToList()
        };
    }

    private static InvoiceLineItemDto ToDto(this InvoiceLineItem item)
    {
        return new InvoiceLineItemDto
        {
            LineNo = item.LineNo,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice.Amount,
            TaxRate = item.TaxRate,
            NetAmount = item.NetAmount.Amount,
            TaxAmount = item.TaxAmount.Amount,
            GrossAmount = item.GrossAmount.Amount
        };
    }

    private static ValidationIssueDto ToDto(this ValidationIssue issue)
    {
        return new ValidationIssueDto
        {
            ValidationIssueId = issue.Id,
            FieldName = issue.FieldName,
            RuleCode = issue.RuleCode,
            Message = issue.Message,
            Severity = issue.Severity.ToString(),
            IsResolved = issue.IsResolved,
            DetectedAt = issue.DetectedAt,
            ResolvedAt = issue.ResolvedAt
        };
    }

    private static InvoiceExtractionResultDto ToDto(this InvoiceExtractionResult result)
    {
        return new InvoiceExtractionResultDto
        {
            ExtractionResultId = result.Id,
            EngineName = result.EngineName,
            ConfidenceScore = result.ConfidenceScore,
            ExtractedAt = result.ExtractedAt,
            IsSuccessful = result.IsSuccessful,
            AttemptNo = result.AttemptNo
        };
    }
}