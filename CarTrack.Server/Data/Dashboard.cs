namespace CarTrack.Server.Data;

public class Dashboard
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<DashboardSection> Sections { get; set; } = [];
}
