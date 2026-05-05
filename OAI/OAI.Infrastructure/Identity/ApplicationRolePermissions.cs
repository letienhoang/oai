namespace OAI.Infrastructure.Identity;

public static class ApplicationRolePermissions
{
    private static readonly IReadOnlyDictionary<string, string[]> PermissionsByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [ApplicationRoles.Administrator] = ApplicationPermissions.All,
            [ApplicationRoles.Accountant] =
            [
                ApplicationPermissions.ViewDashboard,
                ApplicationPermissions.ViewInvoices,
                ApplicationPermissions.UploadInvoices,
                ApplicationPermissions.EditInvoices,
                ApplicationPermissions.ApproveInvoices,
                ApplicationPermissions.RejectInvoices,
                ApplicationPermissions.MoveInvoicesToPendingReview,
                ApplicationPermissions.ViewValidationIssues
            ],
            [ApplicationRoles.Auditor] =
            [
                ApplicationPermissions.ViewDashboard,
                ApplicationPermissions.ViewInvoices,
                ApplicationPermissions.ViewValidationIssues,
                ApplicationPermissions.ViewAuditLogs
            ],
            [ApplicationRoles.Viewer] =
            [
                ApplicationPermissions.ViewDashboard,
                ApplicationPermissions.ViewInvoices,
                ApplicationPermissions.ViewValidationIssues
            ]
        };

    public static IReadOnlyCollection<string> GetPermissions(string roleName)
    {
        return PermissionsByRole.TryGetValue(roleName, out var permissions)
            ? permissions
            : [];
    }

    public static IReadOnlyDictionary<string, string[]> GetAll()
    {
        return PermissionsByRole;
    }
}
