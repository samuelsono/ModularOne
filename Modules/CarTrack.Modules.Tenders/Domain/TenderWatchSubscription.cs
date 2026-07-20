namespace CarTrack.Modules.Tenders;

public class TenderWatchSubscription
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>Null = all sources.</summary>
    public Guid? SourceId { get; set; }

    /// <summary>Null = all queries.</summary>
    public Guid? QueryId { get; set; }

    public bool NotifyInApp { get; set; } = true;

    /// <summary>Stored for Phase 2+; email delivery not wired yet.</summary>
    public bool NotifyEmail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
