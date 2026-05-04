namespace OAI.Domain.Exceptions;

public static class InvoiceDomainErrorCodes
{
    public const string VendorRequired = "Invoice.VendorRequired";
    public const string VendorNotFound = "Invoice.VendorNotFound";
    public const string InvoiceNumberRequired = "Invoice.InvoiceNumberRequired";
    public const string InvoiceNumberAlreadyExists = "Invoice.InvoiceNumberAlreadyExists";
    public const string CurrencyRequired = "Invoice.CurrencyRequired";
    public const string LineItemsRequired = "Invoice.LineItemsRequired";
}
