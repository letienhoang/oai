using OAI.Domain.Entities;

namespace OAI.Application.Validation;

public static class ValidationIssueMessageMapper
{
    public static string? GetMessageCode(ValidationIssue issue)
    {
        return issue.RuleCode switch
        {
            "INV-001" => ValidationMessageCodes.InvoiceMissingLineItems,
            "INV-010" => ValidationMessageCodes.InvoiceSubtotalMismatch,
            "INV-011" => ValidationMessageCodes.InvoiceTaxMismatch,
            "INV-012" => ValidationMessageCodes.InvoiceTotalMismatch,
            _ => null
        };
    }

    public static IReadOnlyDictionary<string, string>? GetMessageParameters(
        ValidationIssue issue,
        Invoice? invoice)
    {
        if (invoice is null)
            return null;

        return issue.RuleCode switch
        {
            "INV-012" => new Dictionary<string, string>
            {
                ["Subtotal"] = invoice.DeclaredSubtotal.Amount.ToString("N0"),
                ["Tax"] = invoice.DeclaredTaxAmount.Amount.ToString("N0"),
                ["Total"] = invoice.DeclaredTotalAmount.Amount.ToString("N0"),
                ["Currency"] = invoice.Currency
            },
            _ => null
        };
    }
}
