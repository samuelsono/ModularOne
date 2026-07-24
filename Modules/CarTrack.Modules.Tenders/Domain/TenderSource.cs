namespace CarTrack.Modules.Tenders;

public class TenderSource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public TenderParserKind ParserKind { get; set; } = TenderParserKind.Auto;

    public bool IsEnabled { get; set; } = true;

    /// <summary>Null means manual runs only.</summary>
    public int? ScrapeIntervalMinutes { get; set; }

    public DateTimeOffset? NextDueAt { get; set; }

    public DateTimeOffset? LastFetchedAt { get; set; }

    public DateTimeOffset? LastSuccessAt { get; set; }

    public string? LastError { get; set; }

    public string? ETag { get; set; }

    public string? LastModifiedHeader { get; set; }

    public string? ContentHash { get; set; }

    public int MaxDetailPagesPerRun { get; set; } = 50;

    public bool RobotsRespect { get; set; } = true;

    public int ConsecutiveFailures { get; set; }

    /// <summary>When set and in the future, scheduled scrapes skip this source.</summary>
    public DateTimeOffset? CircuitOpenedUntil { get; set; }

    public TenderSourceAuthKind AuthKind { get; set; } = TenderSourceAuthKind.None;

    public string? AuthUsername { get; set; }

    /// <summary>Data-protected password, bearer token, or cookie header value (Base64).</summary>
    public string? ProtectedAuthSecret { get; set; }

    /// <summary>OCDS <c>dateFrom</c> override. Null = use global lookback days.</summary>
    public DateOnly? ETendersDateFrom { get; set; }

    /// <summary>OCDS <c>dateTo</c> override. Null = use global forward days.</summary>
    public DateOnly? ETendersDateTo { get; set; }

    /// <summary>OCDS page size override (1–1000). Null = use global default.</summary>
    public int? ETendersPageSize { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
