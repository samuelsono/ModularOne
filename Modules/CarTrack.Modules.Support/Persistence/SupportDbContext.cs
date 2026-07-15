using System.Text.Json;
using System.Text.Json.Serialization;
using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarTrack.Modules.Support;

public sealed class SupportDbContext(DbContextOptions<SupportDbContext> options) : ModuleDbContext(options)
{
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var ticketTypeArrayConverter = CreateTicketTypeArrayConverter();

        builder.Entity<TicketCategory>(entity =>
        {
            entity.ToTable("TicketCategories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasMaxLength(64).IsRequired();
            entity.Property(category => category.Label).HasMaxLength(128).IsRequired();
            entity.Property(category => category.AppliesTo)
                .HasConversion(ticketTypeArrayConverter)
                .HasMaxLength(256)
                .IsRequired();
            entity.HasIndex(category => category.SortOrder);
        });

        builder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("SupportTickets");
            entity.HasKey(ticket => ticket.Id);
            entity.HasIndex(ticket => ticket.SubmittedByUserId);
            entity.HasIndex(ticket => ticket.AssignedToUserId);
            entity.HasIndex(ticket => new { ticket.Status, ticket.Type, ticket.Priority });
            entity.HasIndex(ticket => ticket.CreatedAt);

            entity.Property(ticket => ticket.Subject).HasMaxLength(200).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(4000).IsRequired();
            entity.Property(ticket => ticket.CategoryId).HasMaxLength(64).IsRequired();
            entity.Property(ticket => ticket.BugSeverity).HasMaxLength(32);
            entity.Property(ticket => ticket.StepsToReproduce).HasMaxLength(4000);
            entity.Property(ticket => ticket.ExpectedBehavior).HasMaxLength(1000);
            entity.Property(ticket => ticket.ActualBehavior).HasMaxLength(1000);
            entity.Property(ticket => ticket.BrowserOrEnvironment).HasMaxLength(512);
            entity.Property(ticket => ticket.SubmittedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(ticket => ticket.AssignedToUserId).HasMaxLength(450);
            entity.Property(ticket => ticket.AdminNotes).HasMaxLength(4000);
            // SubmittedByUserId / AssignedToUserId are opaque user ids; no cross-module FK to AspNetUsers.

            entity.HasOne(ticket => ticket.Category)
                .WithMany(category => category.Tickets)
                .HasForeignKey(ticket => ticket.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static ValueConverter<TicketType[], string> CreateTicketTypeArrayConverter()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
        };

        return new ValueConverter<TicketType[], string>(
            value => JsonSerializer.Serialize(value, jsonOptions),
            value => JsonSerializer.Deserialize<TicketType[]>(value, jsonOptions) ?? Array.Empty<TicketType>());
    }
}
