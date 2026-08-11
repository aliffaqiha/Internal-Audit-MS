namespace IAMS.Application.Analytics;

public sealed record AuditStatusDistributionDto(string Status, int Count);
public sealed record FindingRiskDistributionDto(string Risk, int Count);
public sealed record FindingDepartmentDistributionDto(string Department, int Count);
public sealed record FindingCategoryDistributionDto(string Category, int Count);
public sealed record AuditorWorkloadDto(string FullName, int AuditCount);

/// <summary>Aggregated metrics rendered by the management dashboard.</summary>
public sealed record DashboardAnalyticsDto(
    int TotalAudits,
    double AuditProgressPercent,
    int TotalFindings,
    int TotalOpenCaps,
    int CapsDueTomorrow,
    int CapsOverdue,
    double? AverageFindingResolutionDays,
    IReadOnlyList<AuditStatusDistributionDto> AuditStatusDistribution,
    IReadOnlyList<FindingRiskDistributionDto> FindingRiskDistribution,
    IReadOnlyList<FindingDepartmentDistributionDto> FindingDepartmentDistribution,
    IReadOnlyList<FindingCategoryDistributionDto> FindingCategoryDistribution,
    IReadOnlyList<AuditorWorkloadDto> AuditorWorkload)
{
    public static DashboardAnalyticsDto Empty { get; } = new(
        0, 0, 0, 0, 0, 0, null,
        Array.Empty<AuditStatusDistributionDto>(),
        Array.Empty<FindingRiskDistributionDto>(),
        Array.Empty<FindingDepartmentDistributionDto>(),
        Array.Empty<FindingCategoryDistributionDto>(),
        Array.Empty<AuditorWorkloadDto>());
}
