namespace OAI.Infrastructure.SystemHealth;

public sealed class SystemHealthCheckResult
{
    public string EnvironmentName { get; init; } = string.Empty;

    public string MachineName { get; init; } = string.Empty;

    public string FrameworkDescription { get; init; } = string.Empty;

    public DateTimeOffset CheckedAt { get; init; }

    public bool CanConnectToDatabase { get; init; }

    public int PendingMigrationCount { get; init; }

    public IReadOnlyList<string> PendingMigrations { get; init; } = [];

    public int VendorCount { get; init; }

    public int InvoiceCount { get; init; }

    public int DemoInvoiceCount { get; init; }

    public int AuditLogCount { get; init; }

    public int IdentityUserCount { get; init; }

    public int IdentityRoleCount { get; init; }

    public bool DemoDataSeedEnabled { get; init; }

    public string DemoInvoiceNumberPrefix { get; init; } = string.Empty;

    public bool FileStorageConfigured { get; init; }

    public string FileStorageRootPath { get; init; } = string.Empty;

    public bool OcrConfigured { get; init; }

    public string OcrLanguages { get; init; } = string.Empty;

    public bool LlmEnabled { get; init; }

    public string LlmProvider { get; init; } = string.Empty;

    public string LlmModel { get; init; } = string.Empty;

    public bool IsHealthy =>
        CanConnectToDatabase &&
        PendingMigrationCount == 0 &&
        FileStorageConfigured &&
        OcrConfigured;
}
