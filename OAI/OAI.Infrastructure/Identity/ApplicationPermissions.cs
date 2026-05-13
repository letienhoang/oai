namespace OAI.Infrastructure.Identity;

public static class ApplicationPermissions
{
    public const string ViewDashboard = "dashboard.view";

    public const string ViewInvoices = "invoices.view";
    public const string UploadInvoices = "invoices.upload";
    public const string EditInvoices = "invoices.edit";
    public const string ApproveInvoices = "invoices.approve";
    public const string RejectInvoices = "invoices.reject";
    public const string MoveInvoicesToPendingReview = "invoices.move_to_pending_review";

    public const string ViewValidationIssues = "validation_issues.view";

    public const string ViewVendors = "vendors.view";
    public const string ManageVendors = "vendors.manage";

    public const string ViewAuditLogs = "audit_logs.view";

    public const string ViewSettings = "settings.view";

    public const string ManageUsers = "users.manage";
    public const string ManageRoles = "roles.manage";
    
    public const string ManageBackgroundJobs = "background_jobs.manage";

    public static readonly string[] All =
    [
        ViewDashboard,
        ViewInvoices,
        UploadInvoices,
        EditInvoices,
        ApproveInvoices,
        RejectInvoices,
        MoveInvoicesToPendingReview,
        ViewValidationIssues,
        ViewVendors,
        ManageVendors,
        ViewAuditLogs,
        ViewSettings,
        ManageUsers,
        ManageRoles,
        ManageBackgroundJobs
    ];
}
