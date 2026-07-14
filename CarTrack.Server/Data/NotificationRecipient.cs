namespace CarTrack.Server.Data;

public class NotificationRecipient
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public Notification Notification { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }
}
