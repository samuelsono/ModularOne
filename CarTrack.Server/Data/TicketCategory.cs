namespace CarTrack.Server.Data;

public class TicketCategory
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public TicketType[] AppliesTo { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<SupportTicket> Tickets { get; set; } = [];
}
