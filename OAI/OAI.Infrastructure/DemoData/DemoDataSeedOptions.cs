namespace OAI.Infrastructure.DemoData;

public sealed class DemoDataSeedOptions
{
    public bool Enabled { get; set; }

    public bool ResetBeforeSeed { get; set; }

    public string InvoiceNumberPrefix { get; set; } = "DEMO";
}
