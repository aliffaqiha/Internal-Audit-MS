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