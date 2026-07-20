using CarTrack.Core;
using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarTrack.Modules.Leave;

public sealed class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : ModuleDbContext(options)
{
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LeaveType>(entity =>
        {
            entity.ToTable("LeaveTypes");
            entity.HasKey(type => type.Id);
            entity.HasIndex(type => type.Code).IsUnique();

            entity.Property(type => type.Name).HasMaxLength(128).IsRequired();
            entity.Property(type => type.Code).HasMaxLength(32).IsRequired();
            entity.Property(type => type.Color).HasMaxLength(16).IsRequired();
            entity.Property(type => type.AccrualMethod).HasMaxLength(16).IsRequired();
            entity.Property(type => type.EligibleGender)
                .HasMaxLength(16)
                .HasDefaultValue(LeaveTypeGenderEligibility.Any)
                .IsRequired();
            entity.Property(type => type.AnnualEntitlement).HasPrecision(6, 2);
            // CreatedByUserId / UpdatedByUserId are opaque user ids; no cross-module FK.
            ConfigureAuditable(entity);
        });

        builder.Entity<PublicHoliday>(entity =>
        {
            entity.ToTable("PublicHolidays");
            entity.HasKey(holiday => holiday.Id);
            entity.HasIndex(holiday => holiday.Date);

            entity.Property(holiday => holiday.Name).HasMaxLength(128).IsRequired();
            entity.Property(holiday => holiday.Branch).HasMaxLength(128);
            entity.Property(holiday => holiday.ExternalId).HasMaxLength(64);
            entity.HasIndex(holiday => holiday.ExternalId);
        });

        builder.Entity<LeaveBalance>(entity =>
        {
            entity.ToTable("LeaveBalances");
            entity.HasKey(balance => balance.Id);
            entity.HasIndex(balance => new { balance.UserId, balance.LeaveTypeId, balance.CycleStart, balance.CycleEnd }).IsUnique();

            entity.Property(balance => balance.UserId).HasMaxLength(450).IsRequired();

            entity.HasOne(balance => balance.LeaveType)
                .WithMany()
                .HasForeignKey(balance => balance.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            // UserId is opaque; no cross-module FK to AspNetUsers.
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LeaveRequests");
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.ManagerUserId, request.Status });
            entity.HasIndex(request => request.RequesterUserId);

            entity.Property(request => request.RequesterUserId).HasMaxLength(450).IsRequired();
            entity.Property(request => request.ManagerUserId).HasMaxLength(450).IsRequired();
            entity.Property(request => request.Status).HasMaxLength(16).IsRequired();
            entity.Property(request => request.Notes).HasMaxLength(1024);
            entity.Property(request => request.RequesterBranch).HasMaxLength(128);
            entity.Property(request => request.DocumentPath).HasMaxLength(512);
            entity.Property(request => request.DocumentFileName).HasMaxLength(256);
            entity.Property(request => request.DocumentContentType).HasMaxLength(128);
            entity.Property(request => request.WorkingDays).HasPrecision(6, 2);
            entity.Property(request => request.StartDayPortion).HasMaxLength(8).IsRequired();
            entity.Property(request => request.EndDayPortion).HasMaxLength(8).IsRequired();
            entity.Property(request => request.DecidedByUserId).HasMaxLength(450);

            entity.HasOne(request => request.LeaveType)
                .WithMany(type => type.Requests)
                .HasForeignKey(request => request.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            // RequesterUserId / ManagerUserId are opaque; no cross-module FK.

            ConfigureAuditable(entity);
        });
    }

    private static void ConfigureAuditable<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(item => item.CreatedByUserId).HasMaxLength(450);
        entity.Property(item => item.UpdatedByUserId).HasMaxLength(450);
    }
}
