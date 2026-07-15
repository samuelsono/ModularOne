namespace CarTrack.Modules.Leave;

public class PublicHoliday
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateOnly Date { get; set; }

    public bool IsRecurring { get; set; }

    public string? Branch { get; set; }

    public string? ExternalId { get; set; }
}
