using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Common.DataScoping;

/// <summary>
/// Describes what data the current user is allowed to see based on role and department.
/// Full-access roles (Administrator, AuditManager, Auditor, TopManagement) see everything;
/// restricted roles (Auditee) are scoped to their own department.
/// </summary>
public sealed record UserAccessScope(IReadOnlyList<string> Roles, Guid? DepartmentId)
{
    private static readonly string[] FullAccessRoles =
    {
        RoleConstants.Administrator,
        RoleConstants.Manager,
        RoleConstants.Auditor,
        RoleConstants.TopManagement,
    };

    public bool HasFullAccess => Roles.Any(r => FullAccessRoles.Contains(r));
}

public static class CurrentUserAccess
{
    /// <summary>Resolves the current user's roles and department from the database.</summary>
    public static async Task<UserAccessScope> ResolveAsync(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return new UserAccessScope(Array.Empty<string>(), null);

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return new UserAccessScope(Array.Empty<string>(), null);

        return new UserAccessScope(
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.DepartmentId);
    }

    public static IQueryable<Finding> RestrictFindings(
        this IQueryable<Finding> query, UserAccessScope scope)
        => scope.HasFullAccess
            ? query
            : query.Where(f => f.DepartmentId == scope.DepartmentId);

    public static IQueryable<CorrectiveAction> RestrictCaps(
        this IQueryable<CorrectiveAction> query, UserAccessScope scope)
        => scope.HasFullAccess
            ? query
            : query.Where(c => c.Finding != null && c.Finding.DepartmentId == scope.DepartmentId);

    /// <summary>
    /// Ensures a restricted user (e.g. Auditee) can act on a finding in their own department.
    /// Throws <see cref="UnauthorizedAccessException"/> when the finding belongs to another department.
    /// </summary>
    public static void EnsureCanAccessFinding(UserAccessScope scope, Guid? findingDepartmentId)
    {
        if (scope.HasFullAccess)
            return;

        if (findingDepartmentId != scope.DepartmentId)
            throw new UnauthorizedAccessException("Anda tidak memiliki akses ke data di luar departemen Anda.");
    }
}
