namespace CarTrack.Server.Data;

public class DashboardSection
{
    public Guid Id { get; set; }

    public Guid DashboardId { get; set; }

    public Dashboard Dashboard { get; set; } = null!;

    public Guid? ParentSectionId { get; set; }

    public DashboardSection? ParentSection { get; set; }

    public ICollection<DashboardSection> ChildSections { get; set; } = [];

    public string? Title { get; set; }

    public string? Subtitle { get; set; }

    public string LayoutDirection { get; set; } = "Row";

    public string Size { get; set; } = "FullWidth";

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ReportPlacement> ReportPlacements { get; set; } = [];
}
