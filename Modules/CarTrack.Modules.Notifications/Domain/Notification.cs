namespace CarTrack.Modules.Notifications;

public class Notification
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public NotificationCategory Category { get; set; }

    public NotificationActionType? ActionType { get; set; }

    public string? RelatedEntityId { get; set; }

    public string? TargetUserId { get; set; }

    public string? TargetGroupName { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public ICollection<NotificationRecipient> Recipients { get; set; } = [];
}
