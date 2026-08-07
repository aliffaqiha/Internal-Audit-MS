using IAMS.Domain.Common;
using IAMS.Domain.Enums;

namespace IAMS.Domain.Entities;

/// <summary>Corrective Action Plan raised against a finding and tracked to closure.</summary>
public sealed class CorrectiveAction : BaseEntity
{
    public Guid FindingId { get; set; }
    public Finding? Finding { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? PicName { get; set; }
    public DateTimeOffset? TargetDate { get; set; }

    /// <summary>0-100 percentage of completion maintained by the auditee.</summary>
    public int Progress { get; set; }

    public CorrectiveActionStatus Status { get; set; } = CorrectiveActionStatus.Open;

    public string? RejectionReason { get; set; }
    public string? VerificationNote { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }

    // Optional single attachment stored in object storage.
    public string? AttachmentObjectName { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentSizeBytes { get; set; }
    public DateTimeOffset? AttachmentUploadedAt { get; set; }
}