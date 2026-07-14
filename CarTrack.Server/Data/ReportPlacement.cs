namespace CarTrack.Server.Data;

public class ReportPlacement
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public ReportDefinition Report { get; set; } = null!;

    public Guid SectionId { get; set; }

    public DashboardSection Section { get; set; } = null!;

    public int SortOrder { get; set; }

    public string Size { get; set; } = "Medium";

    public bool IsVisible { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
