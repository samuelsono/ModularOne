using System.ComponentModel.DataAnnotations;

namespace CarTrack.Modules.Notifications;

public record NotificationRecipientDto(
    Guid RecipientId,
    Guid NotificationId,
    string Title,
    string Body,
    NotificationCategory Category,
    NotificationActionType? ActionType,
    string? RelatedEntityId,
    bool IsRead,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? ReadAt);

public record UnreadCountResponse(int Count);

public record NotificationTargetDto(
    NotificationTargetType Type,
    string? Value);

public record BroadcastNotificationRequest(
    [property: MaxLength(120)] string Title,
    [property: MaxLength(1000)] string Body,
    NotificationTargetDto Target);

public record AuthUserLookupDto(
    string Id,
    string DisplayName,
    string Email);
