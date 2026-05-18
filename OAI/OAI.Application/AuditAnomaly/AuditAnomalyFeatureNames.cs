namespace OAI.Application.AuditAnomaly;

public static class AuditAnomalyFeatureNames
{
    public const string EditCount = "edit_count";
    public const string ApproveCount = "approve_count";
    public const string RejectCount = "reject_count";
    public const string StatusChangedCount = "status_changed_count";
    public const string DistinctUserCount = "distinct_user_count";
    public const string ValidationCount = "validation_count";
    public const string ExportCount = "export_count";
    public const string SubtotalChangeRatio = "subtotal_change_ratio";
    public const string TaxChangeRatio = "tax_change_ratio";
    public const string TotalChangeRatio = "total_change_ratio";
    public const string VendorChanged = "vendor_changed";
    public const string InvoiceNumberChanged = "invoice_number_changed";
    public const string CurrencyChanged = "currency_changed";
    public const string EditedAfterApproved = "edited_after_approved";
    public const string ExportedAfterRejected = "exported_after_rejected";
    public const string OutsideBusinessHours = "outside_business_hours";
    public const string HasDeletedLineItem = "has_deleted_line_item";
    public const string HasReopenedInvoice = "has_reopened_invoice";
    public const string TotalTaxMismatch = "total_tax_mismatch";
    public const string RepeatedProcessingAttempts = "repeated_processing_attempts";
    public const string MinutesBetweenCreateAndApprove = "minutes_between_create_and_approve";
    public const string MaxUpdatesWithin10Minutes = "max_updates_within_10_minutes";
    public const string AuditDurationMinutes = "audit_duration_minutes";

    public static readonly string[] All =
    [
        EditCount,
        ApproveCount,
        RejectCount,
        StatusChangedCount,
        DistinctUserCount,
        ValidationCount,
        ExportCount,
        SubtotalChangeRatio,
        TaxChangeRatio,
        TotalChangeRatio,
        VendorChanged,
        InvoiceNumberChanged,
        CurrencyChanged,
        EditedAfterApproved,
        ExportedAfterRejected,
        OutsideBusinessHours,
        HasDeletedLineItem,
        HasReopenedInvoice,
        TotalTaxMismatch,
        RepeatedProcessingAttempts,
        MinutesBetweenCreateAndApprove,
        MaxUpdatesWithin10Minutes,
        AuditDurationMinutes
    ];
}
