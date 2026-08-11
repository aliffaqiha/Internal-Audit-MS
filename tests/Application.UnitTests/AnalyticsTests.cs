using IAMS.Application.Analytics;
using IAMS.Domain.Enums;

namespace IAMS.Application.UnitTests;

public class AnalyticsMetricsTests
{
    private static readonly DateTimeOffset Today = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);

    private static DashboardAnalyticsDto Compute(
        IReadOnlyList<AuditPlanStatusSnapshot>? plans = null,
        IReadOnlyList<FindingSnapshot>? findings = null,
        IReadOnlyList<ClosedCapSnapshot>? closedCaps = null,
        int totalOpenCaps = 0,
        IReadOnlyList<DateTime>? openCapTargetDates = null,
        IReadOnlyList<AuditorSnapshot>? workload = null)
        => AnalyticsMetrics.Compute(
            plans ?? [],
            findings ?? [],
            closedCaps ?? [],
            totalOpenCaps,
            openCapTargetDates ?? [],
            workload ?? [],
            Today);

    [Fact]
    public void EmptyData_ReturnsZeroesAndNullResolution()
    {
        var result = Compute();

        Assert.Equal(0, result.TotalAudits);
        Assert.Equal(0, result.AuditProgressPercent);
        Assert.Equal(0, result.TotalFindings);
        Assert.Equal(0, result.TotalOpenCaps);
        Assert.Null(result.AverageFindingResolutionDays);
        Assert.Equal(5, result.AuditStatusDistribution.Count);
        Assert.Equal(4, result.FindingRiskDistribution.Count);
        Assert.Empty(result.FindingDepartmentDistribution);
        Assert.Empty(result.FindingCategoryDistribution);
        Assert.Empty(result.AuditorWorkload);
    }

    [Fact]
    public void AuditProgress_IsCompletedOverTotal()
    {
        var result = Compute(plans:
        [
            new AuditPlanStatusSnapshot(AuditPlanStatus.Completed, 3),
            new AuditPlanStatusSnapshot(AuditPlanStatus.InProgress, 1),
        ]);

        Assert.Equal(4, result.TotalAudits);
        Assert.Equal(75, result.AuditProgressPercent);
    }

    [Fact]
    public void StatusDistribution_IncludesZeroCountStatuses()
    {
        var result = Compute(plans: [new AuditPlanStatusSnapshot(AuditPlanStatus.Completed, 2)]);

        Assert.Equal(2, result.AuditStatusDistribution.Single(s => s.Status == "Completed").Count);
        Assert.Equal(0, result.AuditStatusDistribution.Single(s => s.Status == "Draft").Count);
    }

    [Fact]
    public void Findings_GroupedByRiskDepartmentAndCategory()
    {
        var result = Compute(findings:
        [
            new FindingSnapshot(RiskLevel.High, "Backup", "IT"),
            new FindingSnapshot(RiskLevel.High, "Backup", "IT"),
            new FindingSnapshot(RiskLevel.Critical, "Access", "HR"),
            new FindingSnapshot(RiskLevel.Low, null, null),
        ]);

        Assert.Equal(4, result.TotalFindings);
        Assert.Equal(2, result.FindingRiskDistribution.Single(r => r.Risk == "High").Count);
        Assert.Equal(1, result.FindingRiskDistribution.Single(r => r.Risk == "Critical").Count);

        var it = result.FindingDepartmentDistribution.Single(d => d.Department == "IT");
        Assert.Equal(2, it.Count);
        Assert.Equal(1, result.FindingDepartmentDistribution.Single(d => d.Department == "Tanpa Departemen").Count);

        Assert.Equal(2, result.FindingCategoryDistribution.Single(c => c.Category == "Backup").Count);
        Assert.Equal(1, result.FindingCategoryDistribution.Single(c => c.Category == "Tanpa Kategori").Count);
    }

    [Fact]
    public void CapsDueTomorrowAndOverdue_AreCountedSeparately()
    {
        var result = Compute(
            openCapTargetDates:
            [
                Today.Date,                                  // overdue
                Today.Date.AddDays(-3),                      // overdue
                Today.Date.AddDays(1),                       // due tomorrow
                Today.Date.AddDays(10),                      // future, ignored
            ],
            totalOpenCaps: 4);

        Assert.Equal(4, result.TotalOpenCaps);
        Assert.Equal(1, result.CapsDueTomorrow);
        Assert.Equal(2, result.CapsOverdue);
    }

    [Fact]
    public void AverageResolutionDays_ComputedFromClosedCaps()
    {
        var findingCreated = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var result = Compute(closedCaps:
        [
            new ClosedCapSnapshot(findingCreated.AddDays(10), findingCreated),
            new ClosedCapSnapshot(findingCreated.AddDays(20), findingCreated),
        ]);

        Assert.Equal(15, result.AverageFindingResolutionDays);
    }

    [Fact]
    public void AverageResolutionDays_IgnoresNegativeGaps()
    {
        var findingCreated = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var result = Compute(closedCaps:
        [
            new ClosedCapSnapshot(findingCreated.AddDays(-2), findingCreated),
            new ClosedCapSnapshot(findingCreated.AddDays(4), findingCreated),
        ]);

        Assert.Equal(2, result.AverageFindingResolutionDays);
    }

    [Fact]
    public void AuditorWorkload_SortedByAuditCountDescending()
    {
        var result = Compute(workload:
        [
            new AuditorSnapshot("Budi", 1),
            new AuditorSnapshot("Ani", 5),
        ]);

        Assert.Equal("Ani", result.AuditorWorkload[0].FullName);
        Assert.Equal("Budi", result.AuditorWorkload[1].FullName);
    }
}
