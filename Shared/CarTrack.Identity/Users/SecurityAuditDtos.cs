namespace CarTrack.Server.Users;

public record SecurityAuditLogDto(
    Guid Id,
    string ActorUserId,
    string? ActorDisplayName,
    string? TargetUserId,
    string? TargetDisplayName,
    string Action,
    string? Details,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public record SecurityAuditLogListResponse(IReadOnlyList<SecurityAuditLogDto> Items);
