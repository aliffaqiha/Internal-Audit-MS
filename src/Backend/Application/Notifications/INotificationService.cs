using IAMS.Domain.Enums;

namespace IAMS.Application.Notifications;

/// <summary>Creates persisted in-app notifications (deduped) and pushes them in real time.</summary>
public interface INotificationService
{
    Task SendAsync(
        IReadOnlyCollection<Guid> recipientIds,
        NotificationType type,
        string title,
        string? message = null,
        string? link = null,
        string? dedupeKey = null,
        CancellationToken cancellationToken = default);
}