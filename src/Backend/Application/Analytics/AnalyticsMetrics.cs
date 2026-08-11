using IAMS.Domain.Enums;

namespace IAMS.Application.Analytics;

/// <summary>Lightweight row projections feeding <see cref="AnalyticsMetrics"/> (EF-safe, no navigation materialization).</summary>
public sealed record AuditPlanStatusSnapshot(AuditPlanStatus Status, int Count);
public sealed record FindingSnapshot(RiskLevel RiskLevel, string? Category, string? DepartmentName);
public sealed record ClosedCapSnapshot(DateTimeOffset VerifiedAt, DateTimeOffset FindingCreatedAt);
public sealed record AuditorSnapshot(string FullName, int AuditCount);

/// <summary>Pure computation of dashboard metrics from plain snapshots (unit-testable without EF).</summary>
public static class AnalyticsMetrics
{
    public static DashboardAnalyticsDto Compute(
        IReadOnlyList<AuditPlanStatusSnapshot> plans,
        IReadOnlyList<FindingSnapshot> findings,
        IReadOnlyList<ClosedCapSnapshot> closedCaps,
        int totalOpenCaps,
        IReadOnlyList<DateTime> openCapTargetDates,
        IReadOnlyList<AuditorSnapshot> workload,
        DateTimeOffset today)
    {
        var todayDate = today.Date;

        var totalAudits = plans.Sum(p => p.Count);
        var completed = plans.FirstOrDefault(p => p.Status == AuditPlanStatus.Completed)?.Count ?? 0;
        var progress = totalAudits == 0 ? 0 : Math.Round(completed / (double)totalAudits * 100, 1);

        var dueTomorrow = openCapTargetDates.Count(d => d.Date == todayDate.AddDays(1));
        var overdue = openCapTargetDates.Count(d => d.Date <= todayDate);

        var resolutionDays = closedCaps.Count == 0
            ? (double?)null
            : Math.Round(closedCaps.Average(c => Math.Max(0, (c.VerifiedAt - c.FindingCreatedAt).TotalDays)), 1);

        var statusDistribution = Enum.GetValues<AuditPlanStatus>()
            .Select(s => new AuditStatusDistributionDto(
                s.ToString(),
                plans.Where(p => p.Status == s).Sum(p => p.Count)))
            .ToList();

        var riskDistribution = Enum.GetValues<RiskLevel>()
            .Select(r => new FindingRiskDistributionDto(
                r.ToString(),
                findings.Count(f => f.RiskLevel == r)))
            .ToList();

        var departmentDistribution = findings
            .GroupBy(f => f.DepartmentName ?? "Tanpa Departemen")
            .Select(g => new FindingDepartmentDistributionDto(g.Key, g.Count()))
            .OrderByDescending(d => d.Count)
            .ToList();

        var categoryDistribution = findings
            .GroupBy(f => f.Category ?? "Tanpa Kategori")
            .Select(g => new FindingCategoryDistributionDto(g.Key, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList();

        var auditorWorkload = workload
            .OrderByDescending(w => w.AuditCount)
            .Select(w => new AuditorWorkloadDto(w.FullName, w.AuditCount))
            .ToList();

        return new DashboardAnalyticsDto(
            totalAudits,
            progress,
            findings.Count,
            totalOpenCaps,
            dueTomorrow,
            overdue,
            resolutionDays,
            statusDistribution,
            riskDistribution,
            departmentDistribution,
            categoryDistribution,
            auditorWorkload);
    }
}
