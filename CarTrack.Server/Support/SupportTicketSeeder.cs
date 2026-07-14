using System.Text.Json;
using System.Text.Json.Serialization;
using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Support;

public static class SupportTicketSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly (string Id, string Label, TicketType[] AppliesTo, int SortOrder)[] DefaultCategories =
    [
        ("vehicles", "Vehicle Issues", [TicketType.Ticket, TicketType.BugReport], 1),
        ("drivers", "Driver Management", [TicketType.Ticket], 2),
        ("reports", "Reports & Dashboards", [TicketType.Ticket, TicketType.BugReport], 3),
        ("access", "Access & Permissions", [TicketType.Ticket], 4),
        ("integrations", "Integrations", [TicketType.Ticket, TicketType.BugReport], 5),
        ("general", "General", [TicketType.Ticket, TicketType.Feedback], 6),
        ("ui", "User Interface", [TicketType.Feedback, TicketType.BugReport], 7),
        ("performance", "Performance", [TicketType.BugReport], 8),
        ("data", "Data Accuracy", [TicketType.Ticket, TicketType.BugReport], 9),
    ];

    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.TicketCategories.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var (id, label, appliesTo, sortOrder) in DefaultCategories)
        {
            dbContext.TicketCategories.Add(new TicketCategory
            {
                Id = id,
                Label = label,
                AppliesTo = appliesTo,
                IsActive = true,
                SortOrder = sortOrder,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string SerializeAppliesTo(TicketType[] appliesTo) =>
        JsonSerializer.Serialize(appliesTo, JsonOptions);

    public static TicketType[] DeserializeAppliesTo(string json) =>
        JsonSerializer.Deserialize<TicketType[]>(json, JsonOptions) ?? [];
}
