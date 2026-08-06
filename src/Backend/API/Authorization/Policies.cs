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

    public static AuthorizationOptions Register(this AuthorizationOptions options)
    {
        options.AddPolicy(Administrator, p => p.RequireRole(Domain.Enums.RoleConstants.Administrator));
        options.AddPolicy(AuditManager, p => p.RequireRole(Domain.Enums.RoleConstants.Manager));
        options.AddPolicy(Auditor, p => p.RequireRole(Domain.Enums.RoleConstants.Auditor));
        options.AddPolicy(Auditee, p => p.RequireRole(Domain.Enums.RoleConstants.Auditee));
        options.AddPolicy(TopManagement, p => p.RequireRole(Domain.Enums.RoleConstants.TopManagement));
        options.AddPolicy(DashboardView, p => p.RequireRole(
            Domain.Enums.RoleConstants.TopManagement,
            Domain.Enums.RoleConstants.Manager));

        return options;
    }
}