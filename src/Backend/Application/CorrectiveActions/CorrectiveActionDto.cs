using IAMS.Domain.Entities;
using IAMS.Domain.Enums;

namespace IAMS.Application.CorrectiveActions;

public sealed record CapAttachmentDto(
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    DateTimeOffset? UploadedAt);

public sealed record CorrectiveActionDto(
    Guid Id,
    Guid FindingId,
    string FindingTitle,
    string Action,
    string? PicName,
    DateTimeOffset? TargetDate,
    int Progress,
    CorrectiveActionStatus Status,
    string? RejectionReason,
    string? VerificationNote,
    DateTimeOffset? VerifiedAt,
    CapAttachmentDto? Attachment);

public static class CapState
{
    public static void EnsureTransition(CorrectiveAction cap, CorrectiveActionStatus expected, CorrectiveActionStatus next, string action)
    {
        if (cap.Status != expected)
            throw new InvalidOperationException(
                $"{action} hanya dapat dilakukan dari status '{expected}', saat ini '{cap.Status}'.");
    }
}