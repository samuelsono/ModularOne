namespace CarTrack.Modules.Reporting;

public class ReportDefinition
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public string ReportType { get; set; } = "MetricCard";

    public string Size { get; set; } = "Medium";

    public string TargetTable { get; set; } = "Vehicles";

    public string AggregateFunction { get; set; } = "Count";

    public string? AggregateField { get; set; }

    public string GroupByColumnsJson { get; set; } = "[]";

    public string FiltersJson { get; set; } = "[]";

    public bool ComparisonEnabled { get; set; }

    public string? ChartOptionsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ReportPlacement> Placements { get; set; } = [];
}
