namespace CarTrack.Modules.Tenders;

public interface ITenderSourceService
{
    Task<IReadOnlyList<TenderSourceDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TenderSourceDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenderSourceDto> CreateAsync(
        string userId,
        SaveTenderSourceRequest request,
        CancellationToken cancellationToken = default);

    Task<TenderSourceDto?> UpdateAsync(
        Guid id,
        SaveTenderSourceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITenderQueryService
{
    Task<IReadOnlyList<TenderQueryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TenderQueryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenderQueryDto> CreateAsync(
        string userId,
        SaveTenderQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<TenderQueryDto?> UpdateAsync(
        Guid id,
        SaveTenderQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITenderMatchService
{
    Task<IReadOnlyList<TenderMatchDto>> ListAsync(
        TenderMatchStatus? status = null,
        Guid? sourceId = null,
        string? keyword = null,
        string? search = null,
        bool includeExpired = false,
        TenderResultsScope scope = TenderResultsScope.All,
        string? viewerUserId = null,
        CancellationToken cancellationToken = default);

    Task<TenderMatchDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenderMatchDto?> MarkSeenAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenderMatchDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BulkTenderMatchResult> BulkMarkSeenAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<BulkTenderMatchResult> BulkArchiveAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<(string FileName, byte[] Content)> ExportCsvAsync(
        TenderMatchStatus? status = null,
        Guid? sourceId = null,
        string? keyword = null,
        string? search = null,
        bool includeExpired = false,
        TenderResultsScope scope = TenderResultsScope.All,
        string? viewerUserId = null,
        CancellationToken cancellationToken = default);

    Task<TenderMatchDto?> RefreshDocumentsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITenderScrapeService
{
    Task<TenderScrapeRunDto> EnqueueManualRunAsync(
        string? userId,
        CreateTenderScrapeRunRequest request,
        CancellationToken cancellationToken = default);

    Task<int> EnqueueDueScheduledRunsAsync(CancellationToken cancellationToken = default);

    Task ExpireStaleMatchesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenderScrapeRunDto>> ListRunsAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<TenderScrapeRunDto?> GetRunAsync(Guid id, CancellationToken cancellationToken = default);

    Task ProcessQueuedRunsAsync(CancellationToken cancellationToken = default);
}

public interface ITenderWatchSubscriptionService
{
    Task<TenderWatchSubscriptionDto?> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<TenderWatchSubscriptionDto> UpsertMineAsync(
        string userId,
        SaveTenderWatchSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteMineAsync(string userId, CancellationToken cancellationToken = default);
}
