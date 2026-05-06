namespace OAI.Infrastructure.Identity;

public static class ApplicationPolicies
{
    public const string ViewDashboard = "Policy.Dashboard.View";
    public const string ViewInvoices = "Policy.Invoices.View";
    public const string UploadInvoices = "Policy.Invoices.Upload";
    public const string EditInvoices = "Policy.Invoices.Edit";
    public const string ApproveInvoices = "Policy.Invoices.Approve";
    public const string RejectInvoices = "Policy.Invoices.Reject";
    public const string MoveInvoicesToPendingReview = "Policy.Invoices.MoveToPendingReview";
    public const string ViewValidationIssues = "Policy.ValidationIssues.View";
    public const string ViewVendors = "Policy.Vendors.View";
    public const string ManageVendors = "Policy.Vendors.Manage";
    public const string ViewAuditLogs = "Policy.AuditLogs.View";
    public const string ViewSettings = "Policy.Settings.View";
    public const string ManageUsers = "Policy.Users.Manage";
    public const string ManageRoles = "Policy.Roles.Manage";
}
