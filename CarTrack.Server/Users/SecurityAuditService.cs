using System.Security.Claims;
using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

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

public class SecurityAuditService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : ISecurityAuditService
{
    public async Task LogAsync(
        string action,
        string? targetUserId = null,
        string? targetDisplayName = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var actorUserId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext?.User.FindFirstValue("sub")
            ?? "system";
        var actorDisplayName = httpContext?.User.FindFirstValue(ClaimTypes.Name)
            ?? httpContext?.User.Identity?.Name;

        dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorDisplayName = actorDisplayName,
            TargetUserId = targetUserId,
            TargetDisplayName = targetDisplayName,
            Action = action,
            Details = details,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SecurityAuditLogListResponse> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var cappedLimit = Math.Clamp(limit, 1, 500);
        var items = await dbContext.SecurityAuditLogs
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(cappedLimit)
            .Select(entry => new SecurityAuditLogDto(
                entry.Id,
                entry.ActorUserId,
                entry.ActorDisplayName,
                entry.TargetUserId,
                entry.TargetDisplayName,
                entry.Action,
                entry.Details,
                entry.IpAddress,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);

        return new SecurityAuditLogListResponse(items);
    }
}
