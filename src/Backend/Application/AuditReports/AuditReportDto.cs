namespace IAMS.Application.AuditReports;

/// <summary>Metadata of a generated audit report PDF.</summary>
public sealed record AuditReportDto(
    Guid Id,
    Guid AuditPlanId,
    string ObjectName,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset GeneratedAt);

/// <summary>All data needed to render an audit report PDF.</summary>
public sealed record AuditReportDataDto(
    Guid AuditPlanId,
    string Title,
    string? Objective,
    string? Scope,
    string? Standard,
    string Status,
    string? DepartmentName,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    IReadOnlyList<AuditReportAssignmentDto> Assignments,
    IReadOnlyList<AuditReportChecklistItemDto> ChecklistItems,
    IReadOnlyList<ReportFindingDto> Findings);

public sealed record AuditReportAssignmentDto(
    Guid UserId,
    string Username,
    string FullName,
    string? RoleInPlan);

public sealed record AuditReportChecklistItemDto(
    string? Category,
    string Question,
    bool IsRequired,
    string Status,
    string? Note);

public sealed record ReportFindingDto(
    Guid Id,
    string Title,
    string? Description,
    string? DepartmentName,
    string RiskLevel,
    string? Category,
    string? Recommendation,
    DateTimeOffset? DueDate,
    ReportCorrectiveActionDto? CorrectiveAction);

public sealed record ReportCorrectiveActionDto(
    string Action,
    string? PicName,
    DateTimeOffset? TargetDate,
    int Progress,
    string Status);
