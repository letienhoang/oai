namespace OAI.Domain.Exceptions;

public static class InvoiceDomainErrorCodes
{
    public const string InvoiceIdRequired = "Invoice.InvoiceIdRequired";
    public const string VendorRequired = "Invoice.VendorRequired";
    public const string VendorNotFound = "Invoice.VendorNotFound";
    public const string InvoiceNotFound = "Invoice.InvoiceNotFound";
    public const string InvoiceNumberRequired = "Invoice.InvoiceNumberRequired";
    public const string InvoiceNumberAlreadyExists = "Invoice.InvoiceNumberAlreadyExists";
    public const string CurrencyRequired = "Invoice.CurrencyRequired";
    public const string LineItemsRequired = "Invoice.LineItemsRequired";
    public const string LineItemNotFound = "Invoice.LineItemNotFound";
    public const string InvalidStatusForApprove = "Invoice.InvalidStatusForApprove";
    public const string InvalidStatusForReject = "Invoice.InvalidStatusForReject";
    public const string InvalidStatusForMoveToPendingReview = "Invoice.InvalidStatusForMoveToPendingReview";
}
