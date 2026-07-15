using CarTrack.Core;
using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarTrack.Modules.Expense;

public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : ModuleDbContext(options)
{
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ExpenseCategory>(entity =>
        {
            entity.ToTable("ExpenseCategories");
            entity.HasKey(category => category.Id);
            entity.HasIndex(category => category.Code).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(128).IsRequired();
            entity.Property(category => category.Code).HasMaxLength(32).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(512);
            ConfigureAuditable(entity);
        });

        builder.Entity<ExpenseClaim>(entity =>
        {
            entity.ToTable("ExpenseClaims");
            entity.HasKey(claim => claim.Id);
            entity.HasIndex(claim => new { claim.ManagerUserId, claim.Status });
            entity.HasIndex(claim => claim.RequesterUserId);
            entity.HasIndex(claim => new { claim.RequesterUserId, claim.ExpenseDate });

            entity.Property(claim => claim.RequesterUserId).HasMaxLength(450).IsRequired();
            entity.Property(claim => claim.ManagerUserId).HasMaxLength(450).IsRequired();
            entity.Property(claim => claim.Description).HasMaxLength(512).IsRequired();
            entity.Property(claim => claim.Notes).HasMaxLength(1024);
            entity.Property(claim => claim.Currency).HasMaxLength(8).IsRequired();
            entity.Property(claim => claim.Status).HasMaxLength(16).IsRequired();
            entity.Property(claim => claim.RequesterBranch).HasMaxLength(128);
            entity.Property(claim => claim.Amount).HasPrecision(18, 2);
            entity.Property(claim => claim.PaidByUserId).HasMaxLength(450);
            entity.Property(claim => claim.DecidedByUserId).HasMaxLength(450);

            entity.HasOne(claim => claim.Category)
                .WithMany(category => category.Claims)
                .HasForeignKey(claim => claim.CategoryId)
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
