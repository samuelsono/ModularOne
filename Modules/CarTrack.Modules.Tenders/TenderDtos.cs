namespace CarTrack.Modules.Tenders;

public record TenderSourceDto(
    Guid Id,
    string Name,
    string Url,
    TenderParserKind ParserKind,
    bool IsEnabled,
    int? ScrapeIntervalMinutes,
    DateTimeOffset? NextDueAt,
    DateTimeOffset? LastFetchedAt,
    DateTimeOffset? LastSuccessAt,
    string? LastError,
    int MaxDetailPagesPerRun,
    bool RobotsRespect,
    int ConsecutiveFailures,
    DateTimeOffset? CircuitOpenedUntil,
    bool IsCircuitOpen,
    TenderSourceAuthKind AuthKind,
    string? AuthUsername,
    bool HasAuthSecret,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SaveTenderSourceRequest(
    string Name,
    string Url,
    TenderParserKind ParserKind = TenderParserKind.Auto,
    bool IsEnabled = true,
    int? ScrapeIntervalMinutes = null,
    int MaxDetailPagesPerRun = 50,
    bool RobotsRespect = true,
    TenderSourceAuthKind AuthKind = TenderSourceAuthKind.None,
    string? AuthUsername = null,
    /// <summary>Plaintext secret; omit or null to leave existing secret unchanged. Empty string clears.</summary>
    string? AuthSecret = null);

public record TenderQueryDto(
    Guid Id,
    string Name,
    string[] Keywords,
    TenderQueryMatchMode MatchMode,
    bool IsEnabled,
    Guid[] SourceIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SaveTenderQueryRequest(
    string Name,
    string[] Keywords,
    TenderQueryMatchMode MatchMode = TenderQueryMatchMode.Any,
    bool IsEnabled = true,
    Guid[]? SourceIds = null);

public record CreateTenderScrapeRunRequest(Guid[]? SourceIds = null);

public record TenderScrapeRunSourceDto(
    Guid Id,
    Guid SourceId,
    string? SourceName,
    string Status,
    int? HttpStatus,
    long BytesFetched,
    int DurationMs,
    int ItemsSeen,
    int ItemsMatched,
    int ItemsSkippedKnown,
    int ItemsSkippedExpired,
    string? Message);

public record TenderScrapeRunDto(
    Guid Id,
    TenderScrapeTrigger Trigger,
    TenderScrapeRunStatus Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int SourcesAttempted,
    int SourcesUnchanged,
    int MatchesNew,
    int ItemsSkippedKnown,
    int ItemsSkippedExpired,
    string? ErrorSummary,
    Guid[] SourceIds,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TenderScrapeRunSourceDto> SourceLogs);

public record TenderMatchDto(
    Guid Id,
    Guid SourceId,
    string? SourceName,
    string ExternalKey,
    string CanonicalUrl,
    string Title,
    string? Summary,
    DateOnly? ClosingDate,
    TenderPortalStatus PortalStatus,
    string[] MatchedKeywords,
    string[] DocumentUrls,
    IReadOnlyList<TenderDocumentMeta> DocumentMetadata,
    TenderMatchStatus Status,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset CollectedAt,
    DateTimeOffset? DocumentsRefreshedAt,
    string? OwnerUserId);

public record BulkTenderMatchRequest(Guid[]? Ids);

public record BulkTenderMatchResult(int Updated);

public record TenderWatchSubscriptionDto(
    Guid Id,
    Guid? SourceId,
    Guid? QueryId,
    bool NotifyInApp,
    bool NotifyEmail,
    DateTimeOffset UpdatedAt);

public record SaveTenderWatchSubscriptionRequest(
    Guid? SourceId = null,
    Guid? QueryId = null,
    bool NotifyInApp = true,
    bool NotifyEmail = false);
