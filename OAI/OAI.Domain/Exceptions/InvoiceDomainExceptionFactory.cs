namespace OAI.Domain.Exceptions;

public static class InvoiceDomainExceptionFactory
{
    public static DomainException InvoiceIdRequired()
    {
        return new DomainException(
            message: "InvoiceId is required.",
            code: InvoiceDomainErrorCodes.InvoiceIdRequired);
    }

    public static DomainException VendorRequired()
    {
        return new DomainException(
            message: "Vendor is required.",
            code: InvoiceDomainErrorCodes.VendorRequired);
    }

    public static DomainException VendorNotFound(Guid vendorId)
    {
        return new DomainException(
            message: $"Vendor '{vendorId}' was not found.",
            code: InvoiceDomainErrorCodes.VendorNotFound,
            parameters: new Dictionary<string, string>
            {
                ["VendorId"] = vendorId.ToString()
            });
    }

    public static DomainException InvoiceNotFound(Guid invoiceId)
    {
        return new DomainException(
            message: $"Invoice '{invoiceId}' was not found.",
            code: InvoiceDomainErrorCodes.InvoiceNotFound,
            parameters: new Dictionary<string, string>
            {
                ["InvoiceId"] = invoiceId.ToString()
            });
    }

    public static DomainException InvoiceNumberRequired()
    {
        return new DomainException(
            message: "Invoice number is required.",
            code: InvoiceDomainErrorCodes.InvoiceNumberRequired);
    }

    public static DomainException InvoiceNumberAlreadyExists(string invoiceNumber)
    {
        return new DomainException(
            message: $"Invoice number '{invoiceNumber}' already exists.",
            code: InvoiceDomainErrorCodes.InvoiceNumberAlreadyExists,
            parameters: new Dictionary<string, string>
            {
                ["InvoiceNumber"] = invoiceNumber
            });
    }

    public static DomainException CurrencyRequired()
    {
        return new DomainException(
            message: "Currency is required.",
            code: InvoiceDomainErrorCodes.CurrencyRequired);
    }

    public static DomainException LineItemsRequired()
    {
        return new DomainException(
            message: "At least one line item is required.",
            code: InvoiceDomainErrorCodes.LineItemsRequired);
    }

    public static DomainException LineItemNotFound(Guid lineItemId)
    {
        return new DomainException(
            message: $"Invoice line item '{lineItemId}' was not found.",
            code: InvoiceDomainErrorCodes.LineItemNotFound,
            parameters: new Dictionary<string, string>
            {
                ["LineItemId"] = lineItemId.ToString()
            });
    }

    public static DomainException InvalidStatusForApprove(string currentStatus)
    {
        return new DomainException(
            message: $"Invoice status '{currentStatus}' does not allow approval.",
            code: InvoiceDomainErrorCodes.InvalidStatusForApprove,
            parameters: new Dictionary<string, string>
            {
                ["Status"] = currentStatus
            });
    }

    public static DomainException InvalidStatusForReject(string currentStatus)
    {
        return new DomainException(
            message: $"Invoice status '{currentStatus}' does not allow rejection.",
            code: InvoiceDomainErrorCodes.InvalidStatusForReject,
            parameters: new Dictionary<string, string>
            {
                ["Status"] = currentStatus
            });
    }

    public static DomainException InvalidStatusForMoveToPendingReview(string currentStatus)
    {
        return new DomainException(
            message: $"Invoice status '{currentStatus}' does not allow moving to pending review.",
            code: InvoiceDomainErrorCodes.InvalidStatusForMoveToPendingReview,
            parameters: new Dictionary<string, string>
            {
                ["Status"] = currentStatus
            });
    }
}
