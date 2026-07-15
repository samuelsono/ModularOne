namespace CarTrack.Modules.Notifications;

public class NotificationRecipient
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public Notification Notification { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }
}
