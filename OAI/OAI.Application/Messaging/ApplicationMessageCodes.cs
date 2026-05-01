namespace OAI.Application.Messaging;

public static class ApplicationMessageCodes
{
    public const string InvoiceUploadProcessed = "Action.InvoiceUploadProcessed";
    public const string InvoiceUploadFailed = "Action.InvoiceUploadFailed";
    public const string InvoiceFileStored = "Action.InvoiceFileStored";
    public const string OcrExtractionFailed = "System.OcrExtractionFailed";
    public const string InvoiceCreationFailed = "System.InvoiceCreationFailed";
    public const string InvoiceNotFound = "System.InvoiceNotFound";
    public const string InvalidInvoiceStatus = "System.InvalidInvoiceStatus";
    public const string UnexpectedError = "System.UnexpectedError";
}
