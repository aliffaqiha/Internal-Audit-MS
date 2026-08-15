using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Exceptions;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Xunit;

namespace IAMS.Application.UnitTests;

public class DataScopingTests
{
    private static readonly Guid DeptA = Guid.NewGuid();
    private static readonly Guid DeptB = Guid.NewGuid();

    private static readonly UserAccessScope Restricted =
        new(new[] { RoleConstants.Auditee }, DeptA);

    private static readonly UserAccessScope FullAccess =
        new(new[] { RoleConstants.Administrator }, null);

    [Fact]
    public void RestrictPlans_FullAccess_ReturnsAll()
    {
        var plans = new List<AuditPlan>
        {
            new() { DepartmentId = DeptA },
            new() { DepartmentId = DeptB },
        }.AsQueryable();

        Assert.Equal(2, plans.RestrictPlans(FullAccess).Count());
    }

    [Fact]
    public void RestrictPlans_Restricted_OnlyOwnDepartment()
    {
        var plans = new List<AuditPlan>
        {
            new() { DepartmentId = DeptA },
            new() { DepartmentId = DeptB },
        }.AsQueryable();

        var result = plans.RestrictPlans(Restricted).ToList();
        Assert.Single(result);
        Assert.Equal(DeptA, result[0].DepartmentId);
    }

    [Fact]
    public void RestrictFindings_Restricted_OnlyOwnDepartment()
    {
        var findings = new List<Finding>
        {
            new() { DepartmentId = DeptA },
            new() { DepartmentId = DeptB },
        }.AsQueryable();

        var result = findings.RestrictFindings(Restricted).ToList();
        Assert.Single(result);
        Assert.Equal(DeptA, result[0].DepartmentId);
    }

    [Fact]
    public void EnsureCanAccessPlan_RestrictedOtherDepartment_ThrowsForbidden()
    {
        var ex = Assert.Throws<ForbiddenAccessException>(
            () => CurrentUserAccess.EnsureCanAccessPlan(Restricted, DeptB));
        Assert.Contains("departemen", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCanAccessPlan_RestrictedOwnDepartment_DoesNotThrow()
    {
        CurrentUserAccess.EnsureCanAccessPlan(Restricted, DeptA);
    }

    [Fact]
    public void EnsureCanAccessFinding_RestrictedOtherDepartment_ThrowsForbidden()
    {
        Assert.Throws<ForbiddenAccessException>(
            () => CurrentUserAccess.EnsureCanAccessFinding(Restricted, DeptB));
    }
}
