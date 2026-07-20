using Microsoft.EntityFrameworkCore;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Tenders;

public sealed class TendersDbContext(DbContextOptions<TendersDbContext> options) : ModuleDbContext(options)
{
    public DbSet<TenderSource> TenderSources => Set<TenderSource>();

    public DbSet<TenderQuery> TenderQueries => Set<TenderQuery>();

    public DbSet<TenderScrapeRun> TenderScrapeRuns => Set<TenderScrapeRun>();

    public DbSet<TenderScrapeRunSource> TenderScrapeRunSources => Set<TenderScrapeRunSource>();

    public DbSet<TenderMatch> TenderMatches => Set<TenderMatch>();

    public DbSet<TenderWatchSubscription> TenderWatchSubscriptions => Set<TenderWatchSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TenderSource>(entity =>
        {
            entity.ToTable("TenderSources");
            entity.HasKey(source => source.Id);
            entity.HasIndex(source => source.IsEnabled);
            entity.HasIndex(source => source.NextDueAt);

            entity.Property(source => source.Name).HasMaxLength(200).IsRequired();
            entity.Property(source => source.Url).HasMaxLength(2000).IsRequired();
            entity.Property(source => source.LastError).HasMaxLength(2000);
            entity.Property(source => source.ETag).HasMaxLength(512);
            entity.Property(source => source.LastModifiedHeader).HasMaxLength(256);
            entity.Property(source => source.ContentHash).HasMaxLength(64);
            entity.Property(source => source.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(source => source.ParserKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(source => source.AuthKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(source => source.AuthUsername).HasMaxLength(320);
            entity.Property(source => source.ProtectedAuthSecret).HasMaxLength(4000);
            entity.HasIndex(source => source.CircuitOpenedUntil);
        });

        builder.Entity<TenderQuery>(entity =>
        {
            entity.ToTable("TenderQueries");
            entity.HasKey(query => query.Id);
            entity.HasIndex(query => query.IsEnabled);

            entity.Property(query => query.Name).HasMaxLength(200).IsRequired();
            entity.Property(query => query.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(query => query.MatchMode).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<TenderScrapeRun>(entity =>
        {
            entity.ToTable("TenderScrapeRuns");
            entity.HasKey(run => run.Id);
            entity.HasIndex(run => new { run.Status, run.CreatedAt });

            entity.Property(run => run.ErrorSummary).HasMaxLength(4000);
            entity.Property(run => run.RequestedByUserId).HasMaxLength(450);
            entity.Property(run => run.Trigger).HasConversion<string>().HasMaxLength(32);
            entity.Property(run => run.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasMany(run => run.SourceLogs)
                .WithOne(log => log.Run)
                .HasForeignKey(log => log.RunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TenderScrapeRunSource>(entity =>
        {
            entity.ToTable("TenderScrapeRunSources");
            entity.HasKey(log => log.Id);
            entity.HasIndex(log => log.RunId);
            entity.HasIndex(log => log.SourceId);

            entity.Property(log => log.Status).HasMaxLength(64).IsRequired();
            entity.Property(log => log.Message).HasMaxLength(2000);
        });

        builder.Entity<TenderMatch>(entity =>
        {
            entity.ToTable("TenderMatches");
            entity.HasKey(match => match.Id);
            entity.HasIndex(match => new { match.SourceId, match.ExternalKey }).IsUnique();
            entity.HasIndex(match => match.Status);
            entity.HasIndex(match => match.ClosingDate);
            entity.HasIndex(match => match.FirstSeenAt);
            entity.HasIndex(match => match.OwnerUserId);

            entity.Property(match => match.ExternalKey).HasMaxLength(1000).IsRequired();
            entity.Property(match => match.CanonicalUrl).HasMaxLength(2000).IsRequired();
            entity.Property(match => match.Title).HasMaxLength(1000).IsRequired();
            entity.Property(match => match.Summary).HasMaxLength(4000);
            entity.Property(match => match.DocumentMetadataJson).HasColumnType("text");
            entity.Property(match => match.ContentHash).HasMaxLength(64);
            entity.Property(match => match.OwnerUserId).HasMaxLength(450);
            entity.Property(match => match.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(match => match.PortalStatus).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<TenderWatchSubscription>(entity =>
        {
            entity.ToTable("TenderWatchSubscriptions");
            entity.HasKey(sub => sub.Id);
            entity.HasIndex(sub => sub.UserId).IsUnique();

            entity.Property(sub => sub.UserId).HasMaxLength(450).IsRequired();
        });
    }
}
