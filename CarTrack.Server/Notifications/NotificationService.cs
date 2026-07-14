using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Notifications;

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

public class NotificationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationRecipientDto> CreateSystemNotificationAsync(
        NotificationActionType actionType,
        string title,
        string body,
        string? relatedEntityId = null,
        string? targetUserId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Body = body.Trim(),
            Category = NotificationCategory.SystemAction,
            ActionType = actionType,
            RelatedEntityId = relatedEntityId,
            TargetUserId = targetUserId,
            CreatedAt = DateTime.UtcNow,
        };

        var userIds = await ResolveTargetUserIdsAsync(
            targetUserId is null
                ? new NotificationTargetDto(NotificationTargetType.Everyone, null)
                : new NotificationTargetDto(NotificationTargetType.User, targetUserId),
            cancellationToken);

        var recipients = CreateRecipientRows(notification.Id, userIds);
        notification.Recipients = recipients;

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dtos = recipients.Select(recipient => ToDto(notification, recipient)).ToList();
        await PushToUsersAsync(recipients, notification, cancellationToken);

        return dtos[0];
    }

    public async Task<IReadOnlyList<NotificationRecipientDto>> CreateAdminNotificationAsync(
        string title,
        string body,
        NotificationTargetDto target,
        string createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Body = body.Trim(),
            Category = NotificationCategory.AdminBroadcast,
            ActionType = NotificationActionType.Custom,
            TargetUserId = target.Type == NotificationTargetType.User ? target.Value : null,
            TargetGroupName = target.Type == NotificationTargetType.Group ? target.Value : null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
        };

        var userIds = await ResolveTargetUserIdsAsync(target, cancellationToken);
        if (userIds.Count == 0)
        {
            throw new InvalidOperationException("No recipients matched the selected target.");
        }

        var recipients = CreateRecipientRows(notification.Id, userIds);
        notification.Recipients = recipients;

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dtos = recipients.Select(recipient => ToDto(notification, recipient)).ToList();
        await PushToUsersAsync(recipients, notification, cancellationToken);

        return dtos;
    }

    public async Task<NotificationRecipientDto?> MarkReadAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var recipient = await GetOwnedRecipientAsync(recipientId, userId, cancellationToken);
        if (recipient is null)
        {
            return null;
        }

        if (!recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToDto(recipient.Notification, recipient);
    }

    public async Task<int> MarkAllReadAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var unread = await dbContext.NotificationRecipients
            .Where(recipient => recipient.UserId == userId && !recipient.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var recipient in unread)
        {
            recipient.IsRead = true;
            recipient.ReadAt = now;
        }

        if (unread.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return unread.Count;
    }

    public async Task<NotificationRecipientDto?> ArchiveAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var recipient = await GetOwnedRecipientAsync(recipientId, userId, cancellationToken);
        if (recipient is null)
        {
            return null;
        }

        recipient.IsArchived = true;
        recipient.ArchivedAt = DateTime.UtcNow;
        if (!recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(recipient.Notification, recipient);
    }

    public async Task<bool> DeleteRecipientAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var recipient = await GetOwnedRecipientAsync(recipientId, userId, cancellationToken);
        if (recipient is null)
        {
            return false;
        }

        var notificationId = recipient.NotificationId;
        dbContext.NotificationRecipients.Remove(recipient);
        await dbContext.SaveChangesAsync(cancellationToken);

        var hasRemaining = await dbContext.NotificationRecipients
            .AnyAsync(item => item.NotificationId == notificationId, cancellationToken);

        if (!hasRemaining)
        {
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(item => item.Id == notificationId, cancellationToken);
            if (notification is not null)
            {
                dbContext.Notifications.Remove(notification);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<NotificationRecipientDto>> GetInboxAsync(
        string userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.NotificationRecipients
            .AsNoTracking()
            .Include(recipient => recipient.Notification)
            .Where(recipient => recipient.UserId == userId);

        if (!includeArchived)
        {
            query = query.Where(recipient => !recipient.IsArchived);
        }

        var items = await query
            .OrderByDescending(recipient => recipient.Notification.CreatedAt)
            .ToListAsync(cancellationToken);

        return items
            .Select(recipient => ToDto(recipient.Notification, recipient))
            .ToList();
    }

    public Task<int> GetUnreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationRecipients
            .CountAsync(
                recipient => recipient.UserId == userId
                    && !recipient.IsRead
                    && !recipient.IsArchived,
                cancellationToken);

    private async Task<NotificationRecipient?> GetOwnedRecipientAsync(
        Guid recipientId,
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.NotificationRecipients
            .Include(recipient => recipient.Notification)
            .FirstOrDefaultAsync(
                recipient => recipient.Id == recipientId && recipient.UserId == userId,
                cancellationToken);

    private async Task<IReadOnlyList<string>> ResolveTargetUserIdsAsync(
        NotificationTargetDto target,
        CancellationToken cancellationToken)
    {
        switch (target.Type)
        {
            case NotificationTargetType.Everyone:
                return await dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.IsActive)
                    .Select(user => user.Id)
                    .ToListAsync(cancellationToken);

            case NotificationTargetType.Group:
                if (string.IsNullOrWhiteSpace(target.Value))
                {
                    return [];
                }

                var groupUsers = await userManager.GetUsersInRoleAsync(target.Value.Trim());
                return groupUsers
                    .Where(user => user.IsActive)
                    .Select(user => user.Id)
                    .Distinct()
                    .ToList();

            case NotificationTargetType.User:
                if (string.IsNullOrWhiteSpace(target.Value))
                {
                    return [];
                }

                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.Id == target.Value && item.IsActive,
                        cancellationToken);

                return user is null ? [] : [user.Id];

            default:
                return [];
        }
    }

    private static List<NotificationRecipient> CreateRecipientRows(
        Guid notificationId,
        IReadOnlyList<string> userIds) =>
        userIds
            .Distinct()
            .Select(userId => new NotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                UserId = userId,
                IsRead = false,
                IsArchived = false,
            })
            .ToList();

    private async Task PushToUsersAsync(
        IReadOnlyList<NotificationRecipient> recipients,
        Notification notification,
        CancellationToken cancellationToken)
    {
        foreach (var batch in recipients.Chunk(50))
        {
            var tasks = batch.Select(async recipient =>
            {
                try
                {
                    var dto = ToDto(notification, recipient);
                    await hubContext.Clients
                        .User(recipient.UserId)
                        .SendAsync("notification", dto, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to push notification to user {UserId}", recipient.UserId);
                }
            });

            await Task.WhenAll(tasks);
        }
    }

    private static NotificationRecipientDto ToDto(Notification notification, NotificationRecipient recipient) =>
        new(
            recipient.Id,
            notification.Id,
            notification.Title,
            notification.Body,
            notification.Category,
            notification.ActionType,
            notification.RelatedEntityId,
            recipient.IsRead,
            recipient.IsArchived,
            notification.CreatedAt,
            recipient.ReadAt);
}
