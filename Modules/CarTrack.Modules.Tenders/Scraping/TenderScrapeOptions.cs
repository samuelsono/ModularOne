namespace CarTrack.Modules.Tenders;

public sealed class TenderScrapeOptions
{
    public const string SectionName = "Tenders:Scrape";

    /// <summary>Max concurrent HTTP fetches across all hosts.</summary>
    public int MaxGlobalConcurrency { get; set; } = 4;

    /// <summary>Max concurrent HTTP fetches per hostname.</summary>
    public int MaxPerHostConcurrency { get; set; } = 1;

    /// <summary>Hard cap for a single scrape run.</summary>
    public int MaxRunDurationSeconds { get; set; } = 180;

    /// <summary>Failures before opening the circuit.</summary>
    public int CircuitFailureThreshold { get; set; } = 3;

    /// <summary>How long a circuit stays open before retry.</summary>
    public int CircuitOpenMinutes { get; set; } = 60;

    /// <summary>Short-TTL cache for list/detail HTML (admin re-parse / duplicate fetches).</summary>
    public int FetchCacheTtlMinutes { get; set; } = 10;

    /// <summary>
    /// Archive matches with no closing date after this many days (0 = disabled).
    /// </summary>
    public int UndatedMatchArchiveDays { get; set; } = 90;

    /// <summary>Use Aho–Corasick when total enabled keywords meet this count.</summary>
    public int AhoCorasickKeywordThreshold { get; set; } = 8;

    /// <summary>
    /// When true, <see cref="TenderParserKind.BrowserRendered"/> sources use Playwright/Chromium.
    /// Requires `playwright install chromium` on the host.
    /// </summary>
    public bool EnablePlaywright { get; set; }

    /// <summary>HEAD document URLs to capture Content-Type / filename (no body download).</summary>
    public bool FetchDocumentMetadata { get; set; } = true;

    /// <summary>Max document URLs to probe for metadata per match.</summary>
    public int MaxDocumentMetadataPerMatch { get; set; } = 10;
}
