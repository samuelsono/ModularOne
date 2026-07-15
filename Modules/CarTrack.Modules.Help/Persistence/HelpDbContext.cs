using Microsoft.EntityFrameworkCore;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Help;

public sealed class HelpDbContext(DbContextOptions<HelpDbContext> options) : ModuleDbContext(options)
{
    public DbSet<HelpArticle> HelpArticles => Set<HelpArticle>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<HelpArticle>(entity =>
        {
            entity.ToTable("HelpArticles");
            entity.HasKey(article => article.Id);
            entity.HasIndex(article => article.Slug).IsUnique();
            entity.HasIndex(article => new { article.IsPublished, article.CategoryName });

            entity.Property(article => article.Title).HasMaxLength(200).IsRequired();
            entity.Property(article => article.Slug).HasMaxLength(220).IsRequired();
            entity.Property(article => article.Body).HasMaxLength(50000).IsRequired();
            entity.Property(article => article.CategoryName).HasMaxLength(128);
            entity.Property(article => article.CreatedByUserId).HasMaxLength(450).IsRequired();
            // Tags is string[] — Npgsql maps as text[] by default (PrimitiveCollection).
            // CreatedByUserId is an opaque user id; no cross-module FK to AspNetUsers.
        });
    }
}
