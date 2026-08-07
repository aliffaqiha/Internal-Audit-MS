using FluentValidation.TestHelper;
using IAMS.Application.Audits;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Xunit;

namespace IAMS.Application.UnitTests;

public class AuditStateTests
{
    [Theory]
    [InlineData(AuditPlanStatus.Draft, AuditPlanStatus.Submitted)]
    [InlineData(AuditPlanStatus.Submitted, AuditPlanStatus.Approved)]
    [InlineData(AuditPlanStatus.Approved, AuditPlanStatus.InProgress)]
    [InlineData(AuditPlanStatus.InProgress, AuditPlanStatus.Completed)]
    public void ValidTransition_DoesNotThrow(AuditPlanStatus from, AuditPlanStatus to)
    {
        var plan = new AuditPlan { Status = from };

        AuditState.EnsureTransition(plan, from, to, "transition");
    }

    [Theory]
    [InlineData(AuditPlanStatus.Approved, AuditPlanStatus.Submitted)]
    [InlineData(AuditPlanStatus.Draft, AuditPlanStatus.Approved)]
    [InlineData(AuditPlanStatus.Completed, AuditPlanStatus.InProgress)]
    public void InvalidTransition_Throws(AuditPlanStatus from, AuditPlanStatus to)
    {
        var plan = new AuditPlan { Status = from };

        Assert.Throws<InvalidOperationException>(() =>
            AuditState.EnsureTransition(plan, to, to, "transition"));
    }
}

public class StandardChecklistTemplatesTests
{
    [Fact]
    public void ItStandard_ReturnsTemplateItems()
    {
        var items = StandardChecklistTemplates.ForStandard("IT");

        Assert.NotEmpty(items);
        Assert.Contains(items, i => i.Category == "Firewall");
        Assert.Contains(items, i => i.Category == "Backup");
        Assert.Contains(items, i => i.Category == "Access Control");
        Assert.Contains(items, i => i.Category == "Patch");
    }

    [Fact]
    public void NonItStandard_ReturnsEmpty()
    {
        Assert.Empty(StandardChecklistTemplates.ForStandard("Finance"));
        Assert.Empty(StandardChecklistTemplates.ForStandard(null));
    }
}

public class CreateAuditPlanCommandValidatorTests
{
    private readonly CreateAuditPlanCommandValidator _validator = new();

    [Fact]
    public void EmptyTitle_Fails()
    {
        var result = _validator.TestValidate(new CreateAuditPlanCommand(
            Title: "", Objective: null, Scope: null, Standard: null,
            StartDate: null, EndDate: null, DepartmentId: null,
            Assignments: Array.Empty<AuditAssignmentInput>(),
            ChecklistItems: Array.Empty<AuditChecklistItemInput>()));

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void EndDateBeforeStartDate_Fails()
    {
        var result = _validator.TestValidate(new CreateAuditPlanCommand(
            Title: "A", Objective: null, Scope: null, Standard: null,
            StartDate: new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero),
            EndDate: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            DepartmentId: null,
            Assignments: Array.Empty<AuditAssignmentInput>(),
            ChecklistItems: Array.Empty<AuditChecklistItemInput>()));

        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }
}