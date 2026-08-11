using Microsoft.AspNetCore.Authorization;

namespace IAMS.Api.Authorization;

public static class Policies
{
    public const string Administrator = "Administrator";
    public const string AuditManager = "AuditManager";
    public const string Auditor = "Auditor";
    public const string Auditee = "Auditee";
    public const string TopManagement = "TopManagement";

    public const string DashboardView = "DashboardView";
    public const string AuditPlanner = "AuditPlanner";
    public const string AuditApprover = "AuditApprover";
    public const string FindingManager = "FindingManager";
    public const string CapEditor = "CapEditor";
    public const string CapVerifier = "CapVerifier";

    public static AuthorizationOptions Register(this AuthorizationOptions options)
    {
        options.AddPolicy(Administrator, p => p.RequireRole(Domain.Enums.RoleConstants.Administrator));
        options.AddPolicy(AuditManager, p => p.RequireRole(Domain.Enums.RoleConstants.Manager));
        options.AddPolicy(Auditor, p => p.RequireRole(Domain.Enums.RoleConstants.Auditor));
        options.AddPolicy(Auditee, p => p.RequireRole(Domain.Enums.RoleConstants.Auditee));
        options.AddPolicy(TopManagement, p => p.RequireRole(Domain.Enums.RoleConstants.TopManagement));
        options.AddPolicy(DashboardView, p => p.RequireRole(
            Domain.Enums.RoleConstants.TopManagement,
            Domain.Enums.RoleConstants.Manager,
            Domain.Enums.RoleConstants.Administrator));

        // Audit planning workflow participants.
        options.AddPolicy(AuditPlanner, p => p.RequireRole(
            Domain.Enums.RoleConstants.Auditor,
            Domain.Enums.RoleConstants.Manager,
            Domain.Enums.RoleConstants.Administrator));

        options.AddPolicy(AuditApprover, p => p.RequireRole(
            Domain.Enums.RoleConstants.Manager,
            Domain.Enums.RoleConstants.Administrator));

        // Finding management (create/update/delete/upload evidence).
        options.AddPolicy(FindingManager, p => p.RequireRole(
            Domain.Enums.RoleConstants.Auditor,
            Domain.Enums.RoleConstants.Manager,
            Domain.Enums.RoleConstants.Administrator));

        // CAP editing (auditee fills, auditors/managers/admins can assist).
        options.AddPolicy(CapEditor, p => p.RequireRole(
            Domain.Enums.RoleConstants.Auditee,
            Domain.Enums.RoleConstants.Auditor,
            Domain.Enums.RoleConstants.Manager,
            Domain.Enums.RoleConstants.Administrator));

        // CAP verification (auditor review: approve / reject / reopen).
        options.AddPolicy(CapVerifier, p => p.RequireRole(
            Domain.Enums.RoleConstants.Auditor,
            Domain.Enums.RoleConstants.Manager,
            Domain.Enums.RoleConstants.Administrator));

        return options;
    }
}