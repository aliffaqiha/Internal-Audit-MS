namespace IAMS.Application.Notifications;

/// <summary>In-app notification as delivered to the API client.</summary>
public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Message,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt);

/// <summary>
/// Minimal payload pushed over SignalR. Sensitive text (titles, messages, due dates)
/// is deliberately excluded; the client refetches details through the scoped REST API.
/// </summary>
public sealed record NotificationPushedDto(Guid Id, string Type, string? Link);