namespace OAI.Infrastructure.DemoData;

public sealed class DemoDataSeedResult
{
    public int VendorsCreated { get; init; }

    public int InvoicesCreated { get; init; }

    public int ValidationIssuesCreated { get; init; }

    public int ExtractionResultsCreated { get; init; }

    public bool Skipped { get; init; }

    public string Message { get; init; } = string.Empty;
}
