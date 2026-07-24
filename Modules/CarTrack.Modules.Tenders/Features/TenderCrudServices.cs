using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Tenders;

public sealed class TenderSourceService(
    TendersDbContext dbContext,
    ITenderSourceSecretProtector secrets) : ITenderSourceService
{
    public async Task<IReadOnlyList<TenderSourceDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sources = await dbContext.TenderSources
            .AsNoTracking()
            .OrderBy(source => source.Name)
            .ToListAsync(cancellationToken);

        return sources.Select(TenderMapper.ToDto).ToList();
    }

    public async Task<TenderSourceDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.TenderSources.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return source is null ? null : TenderMapper.ToDto(source);
    }

    public async Task<TenderSourceDto> CreateAsync(
        string userId,
        SaveTenderSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var source = new TenderSource
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        Apply(source, request, secrets, isCreate: true);
        dbContext.TenderSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TenderMapper.ToDto(source);
    }

    public async Task<TenderSourceDto?> UpdateAsync(
        Guid id,
        SaveTenderSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await dbContext.TenderSources.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        Apply(source, request, secrets, isCreate: false);
        source.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return TenderMapper.ToDto(source);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.TenderSources.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (source is null)
        {
            return false;
        }

        dbContext.TenderSources.Remove(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(
        TenderSource source,
        SaveTenderSourceRequest request,
        ITenderSourceSecretProtector secrets,
        bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        source.Name = request.Name.Trim();
        source.Url = TenderUrlNormalizer.Canonicalize(request.Url);
        source.ParserKind = request.ParserKind;
        source.IsEnabled = request.IsEnabled;
        source.ScrapeIntervalMinutes = request.ScrapeIntervalMinutes is > 0
            ? request.ScrapeIntervalMinutes
            : null;
        source.MaxDetailPagesPerRun = request.MaxDetailPagesPerRun <= 0 ? 50 : Math.Min(request.MaxDetailPagesPerRun, 200);
        source.RobotsRespect = request.RobotsRespect;
        source.AuthKind = request.AuthKind;
        source.AuthUsername = string.IsNullOrWhiteSpace(request.AuthUsername)
            ? null
            : request.AuthUsername.Trim();

        if (request.ParserKind is TenderParserKind.ETenders
            || (request.ParserKind is TenderParserKind.Auto
                && Uri.TryCreate(request.Url, UriKind.Absolute, out var sourceUri)
                && sourceUri.Host.Contains("etenders.gov.za", StringComparison.OrdinalIgnoreCase)))
        {
            source.ETendersDateFrom = request.ETendersDateFrom;
            source.ETendersDateTo = request.ETendersDateTo;
            source.ETendersPageSize = request.ETendersPageSize is > 0
                ? Math.Clamp(request.ETendersPageSize.Value, 1, 1000)
                : null;

            if (source.ETendersDateFrom is not null
                && source.ETendersDateTo is not null
                && source.ETendersDateFrom > source.ETendersDateTo)
            {
                throw new InvalidOperationException("ETenders dateFrom must be on or before dateTo.");
            }
        }
        else
        {
            source.ETendersDateFrom = null;
            source.ETendersDateTo = null;
            source.ETendersPageSize = null;
        }

        if (request.AuthKind is TenderSourceAuthKind.None)
        {
            source.ProtectedAuthSecret = null;
            source.AuthUsername = null;
        }
        else if (request.AuthSecret is not null)
        {
            source.ProtectedAuthSecret = string.IsNullOrWhiteSpace(request.AuthSecret)
                ? null
                : secrets.Protect(request.AuthSecret);
        }

        if (request.AuthKind is TenderSourceAuthKind.Basic && string.IsNullOrWhiteSpace(source.AuthUsername))
        {
            throw new InvalidOperationException("Username is required for Basic auth.");
        }

        if (request.AuthKind is not TenderSourceAuthKind.None
            && string.IsNullOrWhiteSpace(source.ProtectedAuthSecret))
        {
            throw new InvalidOperationException(
                isCreate
                    ? "Auth secret is required for the selected auth kind."
                    : "Auth secret is missing. Provide a new secret or clear auth.");
        }

        if (!source.IsEnabled || source.ScrapeIntervalMinutes is not > 0)
        {
            source.NextDueAt = null;
        }
        else if (source.NextDueAt is null)
        {
            TenderScrapeService.ScheduleNextDue(source);
        }
    }
}

public sealed class TenderQueryService(TendersDbContext dbContext) : ITenderQueryService
{
    public async Task<IReadOnlyList<TenderQueryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var queries = await dbContext.TenderQueries
            .AsNoTracking()
            .OrderBy(query => query.Name)
            .ToListAsync(cancellationToken);

        return queries.Select(TenderMapper.ToDto).ToList();
    }

    public async Task<TenderQueryDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = await dbContext.TenderQueries.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return query is null ? null : TenderMapper.ToDto(query);
    }

    public async Task<TenderQueryDto> CreateAsync(
        string userId,
        SaveTenderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = new TenderQuery
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        Apply(query, request);
        dbContext.TenderQueries.Add(query);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TenderMapper.ToDto(query);
    }

    public async Task<TenderQueryDto?> UpdateAsync(
        Guid id,
        SaveTenderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = await dbContext.TenderQueries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (query is null)
        {
            return null;
        }

        Apply(query, request);
        query.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return TenderMapper.ToDto(query);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = await dbContext.TenderQueries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (query is null)
        {
            return false;
        }

        dbContext.TenderQueries.Remove(query);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(TenderQuery query, SaveTenderQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        var keywords = KeywordMatcher.NormalizeKeywords(request.Keywords ?? []);
        if (keywords.Length == 0)
        {
            throw new InvalidOperationException("At least one keyword is required.");
        }

        query.Name = request.Name.Trim();
        query.Keywords = keywords;
        query.MatchMode = request.MatchMode;
        query.IsEnabled = request.IsEnabled;
        query.SourceIds = request.SourceIds?.Distinct().ToArray() ?? [];
    }
}

public sealed class TenderMatchService(
    TendersDbContext dbContext,
    ITenderPageFetcher pageFetcher,
    IHttpClientFactory httpClientFactory,
    TenderHostConcurrencyGate concurrencyGate,
    ITenderSourceSecretProtector secrets,
    IOptions<TenderScrapeOptions> options,
    ILogger<TenderMatchService> logger) : ITenderMatchService
{
    public async Task<IReadOnlyList<TenderMatchDto>> ListAsync(
        TenderMatchStatus? status = null,
        Guid? sourceId = null,
        string? keyword = null,
        string? search = null,
        bool includeExpired = false,
        TenderResultsScope scope = TenderResultsScope.All,
        string? viewerUserId = null,
        CancellationToken cancellationToken = default)
    {
        var matches = await QueryMatches(status, sourceId, keyword, search, includeExpired, scope, viewerUserId)
            .OrderByDescending(match => match.FirstSeenAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return await ToDtosAsync(matches, cancellationToken);
    }

    public async Task<TenderMatchDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await dbContext.TenderMatches.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (match is null)
        {
            return null;
        }

        var dtos = await ToDtosAsync([match], cancellationToken);
        return dtos[0];
    }

    public async Task<TenderMatchDto?> MarkSeenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await dbContext.TenderMatches.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (match is null)
        {
            return null;
        }

        if (match.Status is TenderMatchStatus.New)
        {
            match.Status = TenderMatchStatus.Seen;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var dtos = await ToDtosAsync([match], cancellationToken);
        return dtos[0];
    }

    public async Task<TenderMatchDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await dbContext.TenderMatches.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (match is null)
        {
            return null;
        }

        match.Status = TenderMatchStatus.Archived;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dtos = await ToDtosAsync([match], cancellationToken);
        return dtos[0];
    }

    public async Task<BulkTenderMatchResult> BulkMarkSeenAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new BulkTenderMatchResult(0);
        }

        var matches = await dbContext.TenderMatches
            .Where(match => ids.Contains(match.Id) && match.Status == TenderMatchStatus.New)
            .ToListAsync(cancellationToken);

        foreach (var match in matches)
        {
            match.Status = TenderMatchStatus.Seen;
        }

        if (matches.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BulkTenderMatchResult(matches.Count);
    }

    public async Task<BulkTenderMatchResult> BulkArchiveAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new BulkTenderMatchResult(0);
        }

        var matches = await dbContext.TenderMatches
            .Where(match =>
                ids.Contains(match.Id)
                && match.Status != TenderMatchStatus.Archived
                && match.Status != TenderMatchStatus.Expired)
            .ToListAsync(cancellationToken);

        foreach (var match in matches)
        {
            match.Status = TenderMatchStatus.Archived;
        }

        if (matches.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BulkTenderMatchResult(matches.Count);
    }

    public async Task<(string FileName, byte[] Content)> ExportCsvAsync(
        TenderMatchStatus? status = null,
        Guid? sourceId = null,
        string? keyword = null,
        string? search = null,
        bool includeExpired = false,
        TenderResultsScope scope = TenderResultsScope.All,
        string? viewerUserId = null,
        CancellationToken cancellationToken = default)
    {
        var matches = await QueryMatches(status, sourceId, keyword, search, includeExpired, scope, viewerUserId)
            .OrderByDescending(match => match.FirstSeenAt)
            .Take(5000)
            .ToListAsync(cancellationToken);

        var dtos = await ToDtosAsync(matches, cancellationToken);
        var csv = BuildCsv(dtos);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return ($"tender-results-{stamp}.csv", System.Text.Encoding.UTF8.GetBytes(csv));
    }

    public async Task<TenderMatchDto?> RefreshDocumentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await dbContext.TenderMatches.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (match is null)
        {
            return null;
        }

        var source = await dbContext.TenderSources.FirstOrDefaultAsync(
            item => item.Id == match.SourceId,
            cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException("Match source no longer exists.");
        }

        var urls = match.DocumentUrls.ToList();
        try
        {
            var page = await pageFetcher.FetchHtmlAsync(source, match.CanonicalUrl, cancellationToken);
            if (page is not null && page.StatusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(page.Body))
            {
                var extracted = HtmlTenderParser.ExtractDocumentUrls(page.Body, new Uri(match.CanonicalUrl));
                urls = urls
                    .Concat(extracted)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(30)
                    .ToList();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Document refresh detail fetch failed for match {MatchId}", id);
        }

        match.DocumentUrls = urls.ToArray();
        if (options.Value.FetchDocumentMetadata && urls.Count > 0)
        {
            match.DocumentMetadataJson = TenderDocumentMetadata.Serialize(
                await ProbeDocumentMetadataAsync(source, urls, cancellationToken));
        }

        match.DocumentsRefreshedAt = DateTimeOffset.UtcNow;
        match.CollectedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var dtos = await ToDtosAsync([match], cancellationToken);
        return dtos[0];
    }

    private async Task<IReadOnlyList<TenderDocumentMeta>> ProbeDocumentMetadataAsync(
        TenderSource source,
        IReadOnlyList<string> urls,
        CancellationToken cancellationToken)
    {
        var limit = Math.Max(1, options.Value.MaxDocumentMetadataPerMatch);
        var client = httpClientFactory.CreateClient(TenderScrapeService.HttpClientName);
        var results = new List<TenderDocumentMeta>();

        foreach (var url in urls.Take(limit))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var _ = await concurrencyGate.AcquireAsync(new Uri(url), cancellationToken);
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                HttpTenderPageFetcher.ApplyAuth(request, source, secrets);
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // Some portals reject HEAD — try a ranged GET for headers only.
                    using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpTenderPageFetcher.ApplyAuth(getRequest, source, secrets);
                    getRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                    using var getResponse = await client.SendAsync(
                        getRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);
                    results.Add(MetaFromResponse(url, getResponse));
                    continue;
                }

                results.Add(MetaFromResponse(url, response));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Document metadata probe failed for {Url}", url);
                results.Add(new TenderDocumentMeta(url, null, null, null));
            }
        }

        return results;
    }

    private static TenderDocumentMeta MetaFromResponse(string url, HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? TryFileNameFromUrl(url);
        if (fileName is not null)
        {
            fileName = fileName.Trim('"');
        }

        return new TenderDocumentMeta(url, contentType, fileName, response.Content.Headers.ContentLength);
    }

    private static string? TryFileNameFromUrl(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath;
            var name = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(name) ? null : Uri.UnescapeDataString(name);
        }
        catch
        {
            return null;
        }
    }

    private IQueryable<TenderMatch> QueryMatches(
        TenderMatchStatus? status,
        Guid? sourceId,
        string? keyword,
        string? search,
        bool includeExpired,
        TenderResultsScope scope,
        string? viewerUserId)
    {
        var query = dbContext.TenderMatches.AsNoTracking().AsQueryable();

        if (scope is TenderResultsScope.Mine)
        {
            if (string.IsNullOrWhiteSpace(viewerUserId))
            {
                query = query.Where(_ => false);
            }
            else
            {
                query = query.Where(match => match.OwnerUserId == viewerUserId);
            }
        }

        if (sourceId is not null)
        {
            query = query.Where(match => match.SourceId == sourceId);
        }

        if (status is not null)
        {
            query = query.Where(match => match.Status == status);
        }
        else if (!includeExpired)
        {
            query = query.Where(match => match.Status != TenderMatchStatus.Expired);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            query = query.Where(match =>
                match.MatchedKeywords.Any(item => item.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            query = query.Where(match =>
                match.Title.ToLower().Contains(q)
                || (match.Summary != null && match.Summary.ToLower().Contains(q))
                || match.CanonicalUrl.ToLower().Contains(q)
                || match.MatchedKeywords.Any(item => item.ToLower().Contains(q)));
        }

        return query;
    }

    private async Task<IReadOnlyList<TenderMatchDto>> ToDtosAsync(
        IReadOnlyList<TenderMatch> matches,
        CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
        {
            return [];
        }

        var sourceIds = matches.Select(match => match.SourceId).Distinct().ToList();
        var names = await dbContext.TenderSources.AsNoTracking()
            .Where(source => sourceIds.Contains(source.Id))
            .ToDictionaryAsync(source => source.Id, source => source.Name, cancellationToken);

        return matches.Select(match =>
        {
            names.TryGetValue(match.SourceId, out var sourceName);
            return TenderMapper.ToDto(match, sourceName);
        }).ToList();
    }

    private static string BuildCsv(IReadOnlyList<TenderMatchDto> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Title,Source,Status,ClosingDate,CanonicalUrl,MatchedKeywords,DocumentUrls,DocumentMetadata,FirstSeenAt,OwnerUserId");
        foreach (var row in rows)
        {
            sb.Append(Csv(row.Title)).Append(',');
            sb.Append(Csv(row.SourceName)).Append(',');
            sb.Append(Csv(row.Status.ToString())).Append(',');
            sb.Append(Csv(row.ClosingDate?.ToString("yyyy-MM-dd"))).Append(',');
            sb.Append(Csv(row.CanonicalUrl)).Append(',');
            sb.Append(Csv(string.Join("; ", row.MatchedKeywords))).Append(',');
            sb.Append(Csv(string.Join("; ", row.DocumentUrls))).Append(',');
            sb.Append(Csv(string.Join("; ", row.DocumentMetadata.Select(m =>
                $"{m.FileName ?? m.Url}|{m.ContentType}|{m.ContentLength}")))).Append(',');
            sb.Append(Csv(row.FirstSeenAt.ToString("u"))).Append(',');
            sb.Append(Csv(row.OwnerUserId));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }
}

internal static class TenderMapper
{
    public static TenderSourceDto ToDto(TenderSource source)
    {
        var circuitOpen = source.CircuitOpenedUntil is not null
            && source.CircuitOpenedUntil > DateTimeOffset.UtcNow;

        return new(
            source.Id,
            source.Name,
            source.Url,
            source.ParserKind,
            source.IsEnabled,
            source.ScrapeIntervalMinutes,
            source.NextDueAt,
            source.LastFetchedAt,
            source.LastSuccessAt,
            source.LastError,
            source.MaxDetailPagesPerRun,
            source.RobotsRespect,
            source.ConsecutiveFailures,
            source.CircuitOpenedUntil,
            circuitOpen,
            source.AuthKind,
            source.AuthUsername,
            !string.IsNullOrWhiteSpace(source.ProtectedAuthSecret),
            source.ETendersDateFrom,
            source.ETendersDateTo,
            source.ETendersPageSize,
            source.CreatedAt,
            source.UpdatedAt);
    }

    public static TenderQueryDto ToDto(TenderQuery query) => new(
        query.Id,
        query.Name,
        query.Keywords,
        query.MatchMode,
        query.IsEnabled,
        query.SourceIds,
        query.CreatedAt,
        query.UpdatedAt);

    public static TenderMatchDto ToDto(TenderMatch match, string? sourceName = null) => new(
        match.Id,
        match.SourceId,
        sourceName,
        match.ExternalKey,
        match.CanonicalUrl,
        match.Title,
        match.Summary,
        match.ClosingDate,
        match.PortalStatus,
        match.MatchedKeywords,
        match.DocumentUrls,
        TenderDocumentMetadata.Deserialize(match.DocumentMetadataJson),
        match.Status,
        match.FirstSeenAt,
        match.CollectedAt,
        match.DocumentsRefreshedAt,
        match.OwnerUserId);

    public static TenderScrapeRunDto ToDto(
        TenderScrapeRun run,
        IReadOnlyDictionary<Guid, string>? sourceNames = null) => new(
        run.Id,
        run.Trigger,
        run.Status,
        run.StartedAt,
        run.CompletedAt,
        run.SourcesAttempted,
        run.SourcesUnchanged,
        run.MatchesNew,
        run.ItemsSkippedKnown,
        run.ItemsSkippedExpired,
        run.ErrorSummary,
        run.SourceIds,
        run.CreatedAt,
        run.SourceLogs
            .OrderBy(log => log.Id)
            .Select(log => new TenderScrapeRunSourceDto(
                log.Id,
                log.SourceId,
                sourceNames is not null && sourceNames.TryGetValue(log.SourceId, out var name) ? name : null,
                log.Status,
                log.HttpStatus,
                log.BytesFetched,
                log.DurationMs,
                log.ItemsSeen,
                log.ItemsMatched,
                log.ItemsSkippedKnown,
                log.ItemsSkippedExpired,
                log.Message))
            .ToList());

    public static TenderWatchSubscriptionDto ToDto(TenderWatchSubscription sub) => new(
        sub.Id,
        sub.SourceId,
        sub.QueryId,
        sub.NotifyInApp,
        sub.NotifyEmail,
        sub.UpdatedAt);
}

public sealed class TenderWatchSubscriptionService(TendersDbContext dbContext) : ITenderWatchSubscriptionService
{
    public async Task<TenderWatchSubscriptionDto?> GetMineAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var sub = await dbContext.TenderWatchSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        return sub is null ? null : TenderMapper.ToDto(sub);
    }

    public async Task<TenderWatchSubscriptionDto> UpsertMineAsync(
        string userId,
        SaveTenderWatchSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var sub = await dbContext.TenderWatchSubscriptions
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (sub is null)
        {
            sub = new TenderWatchSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = now,
            };
            dbContext.TenderWatchSubscriptions.Add(sub);
        }

        if (request.SourceId is not null
            && !await dbContext.TenderSources.AnyAsync(source => source.Id == request.SourceId, cancellationToken))
        {
            throw new InvalidOperationException("Source was not found.");
        }

        if (request.QueryId is not null
            && !await dbContext.TenderQueries.AnyAsync(query => query.Id == request.QueryId, cancellationToken))
        {
            throw new InvalidOperationException("Query was not found.");
        }

        sub.SourceId = request.SourceId;
        sub.QueryId = request.QueryId;
        sub.NotifyInApp = request.NotifyInApp;
        sub.NotifyEmail = request.NotifyEmail;
        sub.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return TenderMapper.ToDto(sub);
    }

    public async Task<bool> DeleteMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sub = await dbContext.TenderWatchSubscriptions
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (sub is null)
        {
            return false;
        }

        dbContext.TenderWatchSubscriptions.Remove(sub);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
