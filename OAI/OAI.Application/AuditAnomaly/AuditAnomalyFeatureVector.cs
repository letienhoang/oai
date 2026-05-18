namespace OAI.Application.AuditAnomaly;

public sealed record AuditAnomalyFeatureVector
{
    public int EditCount { get; init; }
    public int ApproveCount { get; init; }
    public int RejectCount { get; init; }
    public int StatusChangedCount { get; init; }
    public int DistinctUserCount { get; init; }
    public int ValidationCount { get; init; }
    public int ExportCount { get; init; }
    public decimal SubtotalChangeRatio { get; init; }
    public decimal TaxChangeRatio { get; init; }
    public decimal TotalChangeRatio { get; init; }
    public int VendorChanged { get; init; }
    public int InvoiceNumberChanged { get; init; }
    public int CurrencyChanged { get; init; }
    public int EditedAfterApproved { get; init; }
    public int ExportedAfterRejected { get; init; }
    public int OutsideBusinessHours { get; init; }
    public int HasDeletedLineItem { get; init; }
    public int HasReopenedInvoice { get; init; }
    public int TotalTaxMismatch { get; init; }
    public int RepeatedProcessingAttempts { get; init; }
    public int MinutesBetweenCreateAndApprove { get; init; }
    public int MaxUpdatesWithin10Minutes { get; init; }
    public int AuditDurationMinutes { get; init; }
}
