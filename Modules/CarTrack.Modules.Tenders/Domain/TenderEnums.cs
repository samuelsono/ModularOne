namespace CarTrack.Modules.Tenders;

public enum TenderParserKind
{
    Auto = 0,
    GenericHtml = 1,
    RssAtom = 2,
    /// <summary>South African eTenders / National Treasury style listing pages.</summary>
    ETenders = 3,
    /// <summary>Fetch via headless Chromium when Playwright is enabled.</summary>
    BrowserRendered = 4,
}

public enum TenderQueryMatchMode
{
    Any = 0,
    All = 1,
    Phrase = 2,
}

public enum TenderScrapeTrigger
{
    Manual = 0,
    Scheduled = 1,
}

public enum TenderScrapeRunStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Partial = 4,
}

public enum TenderMatchStatus
{
    New = 0,
    Seen = 1,
    Archived = 2,
    Dismissed = 3,
    Expired = 4,
}

public enum TenderPortalStatus
{
    Unknown = 0,
    Open = 1,
    Closed = 2,
    Awarded = 3,
    Cancelled = 4,
    Expired = 5,
}

public enum TenderSourceAuthKind
{
    None = 0,
    Basic = 1,
    Bearer = 2,
    CookieHeader = 3,
}

/// <summary>Results visibility filter for list/export.</summary>
public enum TenderResultsScope
{
    /// <summary>Shared org inbox — all matches visible to callers with results.read.</summary>
    All = 0,
    /// <summary>Only matches collected on runs requested by the current user.</summary>
    Mine = 1,
}
