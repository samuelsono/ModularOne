using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public sealed class SettingsDbContext(DbContextOptions<SettingsDbContext> options) : ModuleDbContext(options)
{
    public DbSet<CarTrackSettings> CarTrackSettings => Set<CarTrackSettings>();

    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CarTrackSettings>(entity =>
        {
            entity.ToTable("CarTrackSettings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
            entity.Property(settings => settings.BaseUrl).HasMaxLength(512).IsRequired();
            entity.Property(settings => settings.Username).HasMaxLength(256).IsRequired();
            entity.Property(settings => settings.ProtectedPassword).HasMaxLength(2048);
        });

        builder.Entity<PlatformSettings>(entity =>
        {
            entity.ToTable("PlatformSettings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
            entity.Property(settings => settings.DefaultModuleSlugValue)
                .HasColumnName("DefaultModuleSlug")
                .HasMaxLength(64)
                .IsRequired();
        });
    }
}
