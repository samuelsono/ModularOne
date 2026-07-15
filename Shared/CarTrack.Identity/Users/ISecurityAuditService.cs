namespace CarTrack.Server.Users;

public interface ISecurityAuditService
{
    Task LogAsync(
        string action,
        string? targetUserId = null,
        string? targetDisplayName = null,
        string? details = null,
        CancellationToken cancellationToken = default);

    Task<SecurityAuditLogListResponse> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
