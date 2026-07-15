using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Notifications;

public class NotificationCleanupService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var scope = scopeFactory.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationCleanupService>>();
                logger.LogError(ex, "Notification cleanup failed.");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var cutoff = DateTime.UtcNow.AddHours(-48);

        var staleRecipients = await dbContext.NotificationRecipients
            .Where(recipient => recipient.IsRead
                && recipient.ReadAt != null
                && recipient.ReadAt < cutoff)
            .ToListAsync(cancellationToken);

        if (staleRecipients.Count == 0)
        {
            return;
        }

        var affectedNotificationIds = staleRecipients
            .Select(recipient => recipient.NotificationId)
            .Distinct()
            .ToList();

        dbContext.NotificationRecipients.RemoveRange(staleRecipients);
        await dbContext.SaveChangesAsync(cancellationToken);

        var orphanedIds = await dbContext.Notifications
            .Where(notification => affectedNotificationIds.Contains(notification.Id))
            .Where(notification => !dbContext.NotificationRecipients.Any(
                recipient => recipient.NotificationId == notification.Id))
            .Select(notification => notification.Id)
            .ToListAsync(cancellationToken);

        if (orphanedIds.Count == 0)
        {
            return;
        }

        await dbContext.Notifications
            .Where(notification => orphanedIds.Contains(notification.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
