using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Reporting;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardSection> DashboardSections => Set<DashboardSection>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportPlacement> ReportPlacements => Set<ReportPlacement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Dashboard>(entity =>
        {
            entity.ToTable("Dashboards");
            entity.HasKey(dashboard => dashboard.Id);
            entity.HasIndex(dashboard => dashboard.IsDefault);
            entity.Property(dashboard => dashboard.Name).HasMaxLength(256).IsRequired();
            entity.Property(dashboard => dashboard.Description).HasMaxLength(1024);
        });

        builder.Entity<DashboardSection>(entity =>
        {
            entity.ToTable("DashboardSections");
            entity.HasKey(section => section.Id);
            entity.HasIndex(section => new { section.DashboardId, section.SortOrder });
            entity.HasIndex(section => section.ParentSectionId);
            entity.Property(section => section.Title).HasMaxLength(256);
            entity.Property(section => section.Subtitle).HasMaxLength(512);
            entity.Property(section => section.LayoutDirection).HasMaxLength(16).IsRequired();
            entity.Property(section => section.Size).HasMaxLength(32).IsRequired();

            entity.HasOne(section => section.Dashboard)
                .WithMany(dashboard => dashboard.Sections)
                .HasForeignKey(section => section.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(section => section.ParentSection)
                .WithMany(section => section.ChildSections)
                .HasForeignKey(section => section.ParentSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReportDefinition>(entity =>
        {
            entity.ToTable("ReportDefinitions");
            entity.HasKey(report => report.Id);
            entity.Property(report => report.Name).HasMaxLength(256).IsRequired();
            entity.Property(report => report.Description).HasMaxLength(1024);
            entity.Property(report => report.ReportType).HasMaxLength(32).IsRequired();
            entity.Property(report => report.Size).HasMaxLength(32).IsRequired();
            entity.Property(report => report.TargetTable).HasMaxLength(64).IsRequired();
            entity.Property(report => report.AggregateFunction).HasMaxLength(32).IsRequired();
            entity.Property(report => report.AggregateField).HasMaxLength(128);
            entity.Property(report => report.GroupByColumnsJson).IsRequired();
            entity.Property(report => report.FiltersJson).IsRequired();
        });

        builder.Entity<ReportPlacement>(entity =>
        {
            entity.ToTable("ReportPlacements");
            entity.HasKey(placement => placement.Id);
            entity.HasIndex(placement => new { placement.SectionId, placement.SortOrder });
            entity.HasIndex(placement => new { placement.ReportId, placement.SectionId }).IsUnique();
            entity.Property(placement => placement.Size).HasMaxLength(32).IsRequired();

            entity.HasOne(placement => placement.Report)
                .WithMany(report => report.Placements)
                .HasForeignKey(placement => placement.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(placement => placement.Section)
                .WithMany(section => section.ReportPlacements)
                .HasForeignKey(placement => placement.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
