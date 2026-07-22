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

    public DbSet<WorkLocationType> WorkLocationTypes => Set<WorkLocationType>();

    public DbSet<ScheduleTemplate> ScheduleTemplates => Set<ScheduleTemplate>();

    public DbSet<ScheduleTemplateDay> ScheduleTemplateDays => Set<ScheduleTemplateDay>();

    public DbSet<ScheduleDayOverride> ScheduleDayOverrides => Set<ScheduleDayOverride>();

    public DbSet<AttendanceDay> AttendanceDays => Set<AttendanceDay>();

    public DbSet<AttendanceCollaborator> AttendanceCollaborators => Set<AttendanceCollaborator>();

    public DbSet<AttendancePolicySettings> AttendancePolicySettings => Set<AttendancePolicySettings>();

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

            ConfigureAuditable(entity);
        });

        builder.Entity<WorkLocationType>(entity =>
        {
            entity.ToTable("WorkLocationTypes");
            entity.HasKey(type => type.Id);
            entity.HasIndex(type => type.Code).IsUnique();
            entity.Property(type => type.Code).HasMaxLength(32).IsRequired();
            entity.Property(type => type.Name).HasMaxLength(128).IsRequired();
            entity.Property(type => type.Color).HasMaxLength(16).IsRequired();
        });

        builder.Entity<AttendancePolicySettings>(entity =>
        {
            entity.ToTable("AttendancePolicySettings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
            entity.Property(settings => settings.DefaultAssumption)
                .HasMaxLength(16)
                .IsRequired();
        });

        builder.Entity<ScheduleTemplate>(entity =>
        {
            entity.ToTable("ScheduleTemplates");
            entity.HasKey(template => template.Id);
            entity.HasIndex(template => new { template.UserId, template.EffectiveFrom });
            entity.Property(template => template.UserId).HasMaxLength(450).IsRequired();
            entity.Property(template => template.Notes).HasMaxLength(1024);

            entity.HasMany(template => template.Days)
                .WithOne(day => day.Template)
                .HasForeignKey(day => day.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ScheduleTemplateDay>(entity =>
        {
            entity.ToTable("ScheduleTemplateDays");
            entity.HasKey(day => day.Id);
            entity.HasIndex(day => new { day.TemplateId, day.DayOfWeek }).IsUnique();

            entity.HasOne(day => day.LocationType)
                .WithMany()
                .HasForeignKey(day => day.LocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ScheduleDayOverride>(entity =>
        {
            entity.ToTable("ScheduleDayOverrides");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.Date }).IsUnique();
            entity.Property(item => item.UserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(1024);

            entity.HasOne(item => item.LocationType)
                .WithMany()
                .HasForeignKey(item => item.LocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttendanceDay>(entity =>
        {
            entity.ToTable("AttendanceDays");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.Date }).IsUnique();
            entity.Property(item => item.UserId).HasMaxLength(450).IsRequired();
            entity.Property(item => item.Source).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(1024);
            entity.Property(item => item.RecordedByUserId).HasMaxLength(450).IsRequired();

            entity.HasOne(item => item.PlannedLocationType)
                .WithMany()
                .HasForeignKey(item => item.PlannedLocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.ActualLocationType)
                .WithMany()
                .HasForeignKey(item => item.ActualLocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(item => item.Collaborators)
                .WithOne(collaborator => collaborator.AttendanceDay)
                .HasForeignKey(collaborator => collaborator.AttendanceDayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AttendanceCollaborator>(entity =>
        {
            entity.ToTable("AttendanceCollaborators");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CollaboratorUserId).HasMaxLength(450);
            entity.Property(item => item.ExternalName).HasMaxLength(256);
        });
    }

    private static void ConfigureAuditable<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(item => item.CreatedByUserId).HasMaxLength(450);
        entity.Property(item => item.UpdatedByUserId).HasMaxLength(450);
    }
}
