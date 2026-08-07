using IAMS.Domain.Enums;

namespace IAMS.Application.Findings;

public sealed record FindingEvidenceDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    int Version,
    DateTimeOffset UploadedAt,
    Guid? UploadedBy);

public sealed record FindingDto(
    Guid Id,
    string Title,
    string? Description,
    Guid? DepartmentId,
    string? DepartmentName,
    RiskLevel RiskLevel,
    string? Category,
    string? Recommendation,
    DateTimeOffset? DueDate,
    Guid? AuditPlanId,
    IReadOnlyList<FindingEvidenceDto> Evidences);