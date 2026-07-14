namespace CarTrack.Server.Data;

public class SecurityAuditLog
{
    public Guid Id { get; set; }

    public string ActorUserId { get; set; } = string.Empty;

    public string? ActorDisplayName { get; set; }

    public string? TargetUserId { get; set; }

    public string? TargetDisplayName { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
