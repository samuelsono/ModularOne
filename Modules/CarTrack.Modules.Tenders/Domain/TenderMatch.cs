namespace CarTrack.Modules.Tenders;

public class TenderMatch
{
    public Guid Id { get; set; }

    public Guid SourceId { get; set; }

    /// <summary>Stable collect-once identity (canonical URL or feed GUID).</summary>
    public string ExternalKey { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public DateOnly? ClosingDate { get; set; }

    public TenderPortalStatus PortalStatus { get; set; } = TenderPortalStatus.Unknown;

    public string[] MatchedKeywords { get; set; } = [];

    public string[] DocumentUrls { get; set; } = [];

    /// <summary>JSON array of {url, contentType, fileName, contentLength} from HEAD/GET metadata.</summary>
    public string? DocumentMetadataJson { get; set; }

    public string? ContentHash { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset CollectedAt { get; set; }

    public DateTimeOffset? DocumentsRefreshedAt { get; set; }

    public TenderMatchStatus Status { get; set; } = TenderMatchStatus.New;

    public string? OwnerUserId { get; set; }
}
