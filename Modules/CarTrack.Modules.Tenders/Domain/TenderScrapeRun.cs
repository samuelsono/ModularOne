namespace CarTrack.Modules.Tenders;

public class TenderScrapeRun
{
    public Guid Id { get; set; }

    public TenderScrapeTrigger Trigger { get; set; }

    public TenderScrapeRunStatus Status { get; set; } = TenderScrapeRunStatus.Queued;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public int SourcesAttempted { get; set; }

    public int SourcesUnchanged { get; set; }

    public int MatchesNew { get; set; }

    public int ItemsSkippedKnown { get; set; }

    public int ItemsSkippedExpired { get; set; }

    public string? ErrorSummary { get; set; }

    /// <summary>Optional filter; empty means all enabled sources.</summary>
    public Guid[] SourceIds { get; set; } = [];

    public string? RequestedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TenderScrapeRunSource> SourceLogs { get; set; } = new List<TenderScrapeRunSource>();
}

public class TenderScrapeRunSource
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public TenderScrapeRun Run { get; set; } = null!;

    public Guid SourceId { get; set; }

    public string Status { get; set; } = "Pending";

    public int? HttpStatus { get; set; }

    public long BytesFetched { get; set; }

    public int DurationMs { get; set; }

    public int ItemsSeen { get; set; }

    public int ItemsMatched { get; set; }

    public int ItemsSkippedKnown { get; set; }

    public int ItemsSkippedExpired { get; set; }

    public string? Message { get; set; }
}
