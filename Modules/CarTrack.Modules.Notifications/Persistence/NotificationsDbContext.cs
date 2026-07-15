using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Notifications;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : ModuleDbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Title).HasMaxLength(120).IsRequired();
            entity.Property(notification => notification.Body).HasMaxLength(1000).IsRequired();
            entity.Property(notification => notification.RelatedEntityId).HasMaxLength(128);
            entity.Property(notification => notification.TargetUserId).HasMaxLength(450);
            entity.Property(notification => notification.TargetGroupName).HasMaxLength(128);
            entity.Property(notification => notification.CreatedByUserId).HasMaxLength(450);
            // CreatedByUserId / TargetUserId are opaque user ids; no cross-module FK to AspNetUsers.
            entity.HasIndex(notification => notification.CreatedByUserId);
        });

        builder.Entity<NotificationRecipient>(entity =>
        {
            entity.ToTable("NotificationRecipients");
            entity.HasKey(recipient => recipient.Id);
            entity.Property(recipient => recipient.UserId).HasMaxLength(450).IsRequired();
            // UserId is an opaque user id; no cross-module FK to AspNetUsers.
            entity.HasIndex(recipient => new { recipient.UserId, recipient.IsRead, recipient.IsArchived });
            entity.HasIndex(recipient => recipient.ReadAt);

            entity.HasOne(recipient => recipient.Notification)
                .WithMany(notification => notification.Recipients)
                .HasForeignKey(recipient => recipient.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
