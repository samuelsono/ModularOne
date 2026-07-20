namespace CarTrack.Modules.Tenders;

public class TenderQuery
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Normalized keyword phrases stored as a PostgreSQL text array.</summary>
    public string[] Keywords { get; set; } = [];

    public TenderQueryMatchMode MatchMode { get; set; } = TenderQueryMatchMode.Any;

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Empty = global (all sources). Otherwise opaque source ids this query applies to.
    /// </summary>
    public Guid[] SourceIds { get; set; } = [];

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
