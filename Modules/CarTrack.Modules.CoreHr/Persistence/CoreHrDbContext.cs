using CarTrack.Core;
using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarTrack.Modules.CoreHr;

public sealed class CoreHrDbContext(DbContextOptions<CoreHrDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Position> Positions => Set<Position>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(company => company.Id);
            entity.HasIndex(company => company.Code).IsUnique();
            entity.Property(company => company.Name).HasMaxLength(128).IsRequired();
            entity.Property(company => company.Code).HasMaxLength(32).IsRequired();
            entity.Property(company => company.Description).HasMaxLength(512);
            // CreatedByUserId / UpdatedByUserId are opaque user ids; no cross-module FK to AspNetUsers.
            ConfigureAuditable(entity);
        });

        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(department => department.Id);
            entity.HasIndex(department => new { department.CompanyId, department.Code }).IsUnique();
            entity.Property(department => department.Name).HasMaxLength(128).IsRequired();
            entity.Property(department => department.Code).HasMaxLength(32).IsRequired();
            entity.Property(department => department.Description).HasMaxLength(512);

            entity.HasOne(department => department.Company)
                .WithMany(company => company.Departments)
                .HasForeignKey(department => department.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureAuditable(entity);
        });

        builder.Entity<Position>(entity =>
        {
            entity.ToTable("Positions");
            entity.HasKey(position => position.Id);
            entity.HasIndex(position => new { position.DepartmentId, position.Code }).IsUnique();
            entity.Property(position => position.Name).HasMaxLength(128).IsRequired();
            entity.Property(position => position.Code).HasMaxLength(32).IsRequired();
            entity.Property(position => position.Description).HasMaxLength(512);

            entity.HasOne(position => position.Department)
                .WithMany(department => department.Positions)
                .HasForeignKey(position => position.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

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
