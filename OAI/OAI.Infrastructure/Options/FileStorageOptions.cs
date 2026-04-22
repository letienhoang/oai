namespace OAI.Infrastructure.Options;

public sealed class FileStorageOptions
{
    public string? BasePath { get; set; } = null;
    public string RootPath { get; set; } = "storage";
    public string InvoiceFolder { get; set; } = "invoices";
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB
}