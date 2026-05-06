namespace OAI.Infrastructure.DemoData;

public sealed class DemoDataResetResult
{
    public int VendorsDeleted { get; init; }

    public int InvoicesDeleted { get; init; }

    public int LineItemsDeleted { get; init; }

    public int ValidationIssuesDeleted { get; init; }

    public int ExtractionResultsDeleted { get; init; }

    public bool Skipped { get; init; }

    public string Message { get; init; } = string.Empty;
}
