namespace OAI.Application.Validation;

public static class ValidationMessageCodes
{
    public const string InvoiceTotalMismatch = "Validation.InvoiceTotalMismatch";
    public const string InvoiceSubtotalMismatch = "Validation.InvoiceSubtotalMismatch";
    public const string InvoiceTaxMismatch = "Validation.InvoiceTaxMismatch";
    public const string InvoiceMissingNumber = "Validation.InvoiceMissingNumber";
    public const string InvoiceMissingVendor = "Validation.InvoiceMissingVendor";
    public const string InvoiceMissingIssueDate = "Validation.InvoiceMissingIssueDate";
    public const string InvoiceMissingLineItems = "Validation.InvoiceMissingLineItems";
    public const string InvoiceLineAmountMismatch = "Validation.InvoiceLineAmountMismatch";
    public const string InvoiceDueDateBeforeIssueDate = "Validation.InvoiceDueDateBeforeIssueDate";
}
