namespace CarTrack.Server.Data;

public class SupportTicket
{
    public Guid Id { get; set; }

    public TicketType Type { get; set; }

    public TicketStatus Status { get; set; }

    public TicketPriority Priority { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public TicketCategory Category { get; set; } = null!;

    public string? BugSeverity { get; set; }

    public string? StepsToReproduce { get; set; }

    public string? ExpectedBehavior { get; set; }

    public string? ActualBehavior { get; set; }

    public string? BrowserOrEnvironment { get; set; }

    public int? SatisfactionRating { get; set; }

    public string SubmittedByUserId { get; set; } = string.Empty;

    public ApplicationUser SubmittedBy { get; set; } = null!;

    public string? AssignedToUserId { get; set; }

    public ApplicationUser? AssignedTo { get; set; }

    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
