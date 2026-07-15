namespace CarTrack.Modules.Notifications;

public interface INotificationService
{
    Task<NotificationRecipientDto> CreateSystemNotificationAsync(
        NotificationActionType actionType,
        string title,
        string body,
        string? relatedEntityId = null,
        string? targetUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRecipientDto>> CreateAdminNotificationAsync(
        string title,
        string body,
        NotificationTargetDto target,
        string createdByUserId,
        CancellationToken cancellationToken = default);

    Task<NotificationRecipientDto?> MarkReadAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<NotificationRecipientDto?> ArchiveAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteRecipientAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRecipientDto>> GetInboxAsync(
        string userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
