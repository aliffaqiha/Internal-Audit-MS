using Hangfire.AspNetCore;
using Hangfire.Dashboard;
using IAMS.Domain.Enums;

namespace IAMS.Api.Jobs;

/// <summary>Restricts the Hangfire dashboard to authenticated managers/admins.</summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var user = http?.User;

        return user?.Identity?.IsAuthenticated == true
            && (user.IsInRole(RoleConstants.Administrator)
                || user.IsInRole(RoleConstants.Manager));
    }
}
