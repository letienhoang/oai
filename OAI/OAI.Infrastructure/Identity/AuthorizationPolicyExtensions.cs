using Microsoft.AspNetCore.Authorization;

namespace OAI.Infrastructure.Identity;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationOptions AddOaiPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(ApplicationPolicies.ViewDashboard, policy =>
            policy.RequirePermission(ApplicationPermissions.ViewDashboard));
        options.AddPolicy(ApplicationPolicies.ViewInvoices, policy =>
            policy.RequirePermission(ApplicationPermissions.ViewInvoices));
        options.AddPolicy(ApplicationPolicies.UploadInvoices, policy =>
            policy.RequirePermission(ApplicationPermissions.UploadInvoices));
        options.AddPolicy(ApplicationPolicies.EditInvoices, policy =>
            policy.RequirePermission(ApplicationPermissions.EditInvoices));
        options.AddPolicy(ApplicationPolicies.ApproveInvoices, policy =>
            policy.RequirePermission(ApplicationPermissions.ApproveInvoices));
        options.AddPolicy(ApplicationPolicies.RejectInvoices, policy =>
            policy.RequirePermission(ApplicationPermissions.RejectInvoices));
        options.AddPolicy(ApplicationPolicies.MoveInvoicesToPendingReview, policy =>
            policy.RequirePermission(ApplicationPermissions.MoveInvoicesToPendingReview));
        options.AddPolicy(ApplicationPolicies.ViewValidationIssues, policy =>
            policy.RequirePermission(ApplicationPermissions.ViewValidationIssues));
        options.AddPolicy(ApplicationPolicies.ViewVendors, policy =>
            policy.RequirePermission(ApplicationPermissions.ViewVendors));
        options.AddPolicy(ApplicationPolicies.ManageVendors, policy =>
            policy.RequirePermission(ApplicationPermissions.ManageVendors));
        options.AddPolicy(ApplicationPolicies.ViewAuditLogs, policy =>
            policy.RequirePermission(ApplicationPermissions.ViewAuditLogs));
        options.AddPolicy(ApplicationPolicies.ViewSettings, policy =>
            policy.RequirePermission(ApplicationPermissions.ViewSettings));
        options.AddPolicy(ApplicationPolicies.ManageUsers, policy =>
            policy.RequirePermission(ApplicationPermissions.ManageUsers));
        options.AddPolicy(ApplicationPolicies.ManageRoles, policy =>
            policy.RequirePermission(ApplicationPermissions.ManageRoles));

        return options;
    }

    private static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder policy,
        string permission)
    {
        return policy
            .RequireAuthenticatedUser()
            .RequireClaim(ApplicationClaimTypes.Permission, permission);
    }
}
