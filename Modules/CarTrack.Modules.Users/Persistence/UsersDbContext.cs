using CarTrack.Core;
using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarTrack.Modules.Users;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : ModuleDbContext(options)
{
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();

    public DbSet<DriverProfileLink> DriverProfileLinks => Set<DriverProfileLink>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });

            entity.Property(token => token.TokenHash)
                .HasMaxLength(128)
                .IsRequired();
            // UserId is opaque; no cross-module FK to AspNetUsers.
        });

        builder.Entity<StaffProfile>(entity =>
        {
            entity.ToTable("StaffProfiles");
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => profile.ManagerUserId);

            entity.Property(profile => profile.EmployeeNumber).HasMaxLength(64);
            entity.Property(profile => profile.JobTitle).HasMaxLength(128);
            entity.Property(profile => profile.Department).HasMaxLength(128);
            entity.Property(profile => profile.Branch).HasMaxLength(128);
            entity.Property(profile => profile.Gender)
                .HasMaxLength(16)
                .HasDefaultValue("Unspecified")
                .IsRequired();
            entity.Property(profile => profile.EmploymentStatus).HasMaxLength(32).IsRequired();

            entity.HasIndex(profile => profile.CompanyId);
            entity.HasIndex(profile => profile.DepartmentId);
            entity.HasIndex(profile => profile.PositionId);
            // UserId / ManagerUserId / CoreHr ids are opaque; no cross-module FKs.
            ConfigureAuditable(entity);
        });

        builder.Entity<DriverProfileLink>(entity =>
        {
            entity.ToTable("DriverProfileLinks");
            entity.HasKey(link => link.Id);
            entity.HasIndex(link => link.UserId).IsUnique();
            entity.HasIndex(link => link.DriverId).IsUnique();
            // UserId / DriverId are opaque; no cross-module FKs.
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(permission => permission.Id);
            entity.HasIndex(permission => permission.Key).IsUnique();

            entity.Property(permission => permission.Key).HasMaxLength(128).IsRequired();
            entity.Property(permission => permission.ModuleSlug).HasMaxLength(64).IsRequired();
            entity.Property(permission => permission.SubmoduleSlug).HasMaxLength(64).IsRequired();
            entity.Property(permission => permission.Action).HasMaxLength(16).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(512);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(mapping => mapping.Id);
            entity.HasIndex(mapping => new { mapping.RoleId, mapping.PermissionId }).IsUnique();

            entity.HasOne(mapping => mapping.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(mapping => mapping.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            // RoleId is opaque AspNetRoles id; no cross-module FK.
        });

        builder.Entity<SecurityAuditLog>(entity =>
        {
            entity.ToTable("SecurityAuditLogs");
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.CreatedAt);
            entity.HasIndex(entry => entry.TargetUserId);
            entity.HasIndex(entry => entry.Action);

            entity.Property(entry => entry.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(entry => entry.ActorDisplayName).HasMaxLength(256);
            entity.Property(entry => entry.TargetUserId).HasMaxLength(450);
            entity.Property(entry => entry.TargetDisplayName).HasMaxLength(256);
            entity.Property(entry => entry.Action).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.Details).HasMaxLength(2048);
            entity.Property(entry => entry.IpAddress).HasMaxLength(64);
        });
    }

    private static void ConfigureAuditable<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(item => item.CreatedByUserId).HasMaxLength(450);
        entity.Property(item => item.UpdatedByUserId).HasMaxLength(450);
    }
}
