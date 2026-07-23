using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using CarTrack.Infrastructure.Messaging;
using CarTrack.Infrastructure.Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Tenders;

public sealed class TenderScrapeService(
    TendersDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    TenderParserRegistry parserRegistry,
    IEventBus eventBus,
    TenderHostConcurrencyGate concurrencyGate,
    ITenderFetchCache fetchCache,
    TenderKnownKeyCache knownKeyCache,
    ITenderPageFetcher pageFetcher,
    IETendersOcdsFetcher eTendersOcdsFetcher,
    ITenderSourceSecretProtector secrets,
    IOptions<TenderScrapeOptions> options,
    ILogger<TenderScrapeService> logger) : ITenderScrapeService
{
    public const string HttpClientName = nameof(TenderScrapeService);
    private static readonly Random Jitter = Random.Shared;

    public async Task<TenderScrapeRunDto> EnqueueManualRunAsync(
        string? userId,
        CreateTenderScrapeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourceFilter = request.SourceIds ?? [];
        var enabledCount = await dbContext.TenderSources.CountAsync(
            source => source.IsEnabled
                && (sourceFilter.Length == 0 || sourceFilter.Contains(source.Id)),
            cancellationToken);

        if (enabledCount == 0)
        {
            throw new InvalidOperationException("No enabled sources match the run request.");
        }

        var queryCount = await dbContext.TenderQueries.CountAsync(query => query.IsEnabled, cancellationToken);
        if (queryCount == 0)
        {
            throw new InvalidOperationException("At least one enabled keyword query is required before running a scrape.");
        }

        var run = new TenderScrapeRun
        {
            Id = Guid.NewGuid(),
            Trigger = TenderScrapeTrigger.Manual,
            Status = TenderScrapeRunStatus.Queued,
            SourceIds = request.SourceIds?.Distinct().ToArray() ?? [],
            RequestedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.TenderScrapeRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapRunAsync(run.Id, cancellationToken) ?? TenderMapper.ToDto(run);
    }

    public async Task<int> EnqueueDueScheduledRunsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var dueSources = await dbContext.TenderSources
            .Where(source =>
                source.IsEnabled
                && source.ScrapeIntervalMinutes != null
                && source.ScrapeIntervalMinutes > 0
                && (source.NextDueAt == null || source.NextDueAt <= now)
                && (source.CircuitOpenedUntil == null || source.CircuitOpenedUntil <= now))
            .OrderBy(source => source.NextDueAt ?? DateTimeOffset.MinValue)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (dueSources.Count == 0)
        {
            return 0;
        }

        var hasQueries = await dbContext.TenderQueries.AnyAsync(query => query.IsEnabled, cancellationToken);
        if (!hasQueries)
        {
            logger.LogDebug("Skipping scheduled tender scrape — no enabled queries.");
            foreach (var source in dueSources)
            {
                ScheduleNextDue(source);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var enqueued = 0;
        foreach (var source in dueSources)
        {
            ScheduleNextDue(source);

            dbContext.TenderScrapeRuns.Add(new TenderScrapeRun
            {
                Id = Guid.NewGuid(),
                Trigger = TenderScrapeTrigger.Scheduled,
                Status = TenderScrapeRunStatus.Queued,
                SourceIds = [source.Id],
                CreatedAt = now,
            });
            enqueued++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return enqueued;
    }

    public async Task ExpireStaleMatchesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stale = await dbContext.TenderMatches
            .Where(match =>
                match.Status != TenderMatchStatus.Expired
                && match.Status != TenderMatchStatus.Archived
                && match.ClosingDate != null
                && match.ClosingDate < today)
            .ToListAsync(cancellationToken);

        foreach (var match in stale)
        {
            match.Status = TenderMatchStatus.Expired;
            match.PortalStatus = TenderPortalStatus.Expired;
        }

        var archiveDays = options.Value.UndatedMatchArchiveDays;
        var archivedCount = 0;
        if (archiveDays > 0)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-archiveDays);
            var undated = await dbContext.TenderMatches
                .Where(match =>
                    match.ClosingDate == null
                    && match.Status != TenderMatchStatus.Archived
                    && match.Status != TenderMatchStatus.Expired
                    && match.FirstSeenAt < cutoff)
                .ToListAsync(cancellationToken);

            foreach (var match in undated)
            {
                match.Status = TenderMatchStatus.Archived;
                archivedCount++;
            }
        }

        if (stale.Count > 0 || archivedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Tender maintenance: expired {Expired}, archived undated {Archived}.",
                stale.Count,
                archivedCount);
        }
    }

    public async Task<IReadOnlyList<TenderScrapeRunDto>> ListRunsAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var runs = await dbContext.TenderScrapeRuns
            .AsNoTracking()
            .Include(run => run.SourceLogs)
            .OrderByDescending(run => run.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

        var names = await LoadSourceNamesAsync(runs.SelectMany(r => r.SourceLogs.Select(l => l.SourceId)), cancellationToken);
        return runs.Select(run => TenderMapper.ToDto(run, names)).ToList();
    }

    public async Task<TenderScrapeRunDto?> GetRunAsync(Guid id, CancellationToken cancellationToken = default) =>
        await MapRunAsync(id, cancellationToken);

    public async Task ProcessQueuedRunsAsync(CancellationToken cancellationToken = default)
    {
        var queuedIds = await dbContext.TenderScrapeRuns
            .Where(run => run.Status == TenderScrapeRunStatus.Queued)
            .OrderBy(run => run.CreatedAt)
            .Select(run => run.Id)
            .Take(3)
            .ToListAsync(cancellationToken);

        foreach (var runId in queuedIds)
        {
            await ProcessRunAsync(runId, cancellationToken);
        }
    }

    private async Task ProcessRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await dbContext.TenderScrapeRuns
            .Include(item => item.SourceLogs)
            .FirstOrDefaultAsync(item => item.Id == runId, cancellationToken);

        if (run is null || run.Status != TenderScrapeRunStatus.Queued)
        {
            return;
        }

        run.Status = TenderScrapeRunStatus.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(30, options.Value.MaxRunDurationSeconds)));

        try
        {
            await ExpireStaleMatchesAsync(timeoutCts.Token);

            var sourcesQuery = dbContext.TenderSources.Where(source => source.IsEnabled);
            if (run.SourceIds.Length > 0)
            {
                sourcesQuery = sourcesQuery.Where(source => run.SourceIds.Contains(source.Id));
            }

            var sources = await sourcesQuery.OrderBy(source => source.Name).ToListAsync(timeoutCts.Token);
            var queries = await dbContext.TenderQueries
                .Where(query => query.IsEnabled)
                .ToListAsync(timeoutCts.Token);

            var keywordIndex = KeywordMatcher.Compile(
                queries.Select(q => (q.Keywords, q.MatchMode)),
                options.Value.AhoCorasickKeywordThreshold);

            var errors = new List<string>();
            var newMatches = new List<(TenderMatch Match, TenderSource Source)>();

            foreach (var source in sources)
            {
                timeoutCts.Token.ThrowIfCancellationRequested();

                if (source.CircuitOpenedUntil is not null && source.CircuitOpenedUntil > DateTimeOffset.UtcNow)
                {
                    run.SourcesAttempted++;
                    dbContext.TenderScrapeRunSources.Add(new TenderScrapeRunSource
                    {
                        Id = Guid.NewGuid(),
                        RunId = run.Id,
                        SourceId = source.Id,
                        Status = "CircuitOpen",
                        Message = $"Circuit open until {source.CircuitOpenedUntil:u}",
                    });
                    continue;
                }

                try
                {
                    var created = await ScrapeSourceAsync(run, source, queries, keywordIndex, timeoutCts.Token);
                    newMatches.AddRange(created.Select(match => (match, source)));
                    RecordSuccess(source);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    errors.Add($"{source.Name}: run time limit reached");
                    RecordFailure(source, "Run time limit reached");
                    dbContext.TenderScrapeRunSources.Add(new TenderScrapeRunSource
                    {
                        Id = Guid.NewGuid(),
                        RunId = run.Id,
                        SourceId = source.Id,
                        Status = "TimedOut",
                        Message = "Max run duration exceeded",
                    });
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Tender scrape failed for source {SourceId}", source.Id);
                    errors.Add($"{source.Name}: {ex.Message}");
                    RecordFailure(source, Truncate(ex.Message, 1900)!);
                    if (source.ScrapeIntervalMinutes is > 0)
                    {
                        ScheduleNextDue(source);
                    }
                }
            }

            run.CompletedAt = DateTimeOffset.UtcNow;
            run.ErrorSummary = errors.Count == 0 ? null : Truncate(string.Join("; ", errors), 3900);
            run.Status = errors.Count == 0
                ? TenderScrapeRunStatus.Succeeded
                : run.MatchesNew > 0 || run.SourcesAttempted > errors.Count
                    ? TenderScrapeRunStatus.Partial
                    : TenderScrapeRunStatus.Failed;

            await dbContext.SaveChangesAsync(cancellationToken);
            await NotifyNewMatchesAsync(newMatches, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            run.Status = TenderScrapeRunStatus.Partial;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.ErrorSummary = "Run stopped: max duration exceeded.";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Tender scrape run {RunId} failed", runId);
            run.Status = TenderScrapeRunStatus.Failed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.ErrorSummary = Truncate(ex.Message, 3900);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<TenderMatch>> ScrapeSourceAsync(
        TenderScrapeRun run,
        TenderSource source,
        IReadOnlyList<TenderQuery> queries,
        CompiledKeywordIndex keywordIndex,
        CancellationToken cancellationToken)
    {
        run.SourcesAttempted++;
        var stopwatch = Stopwatch.StartNew();
        var createdMatches = new List<TenderMatch>();
        var log = new TenderScrapeRunSource
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            SourceId = source.Id,
            Status = "Running",
        };
        dbContext.TenderScrapeRunSources.Add(log);

        var sourceUri = new Uri(source.Url);
        await using var _ = await concurrencyGate.AcquireAsync(sourceUri, cancellationToken);

        try
        {
            string body;
            string contentType;
            int? httpStatus;

            var cached = await fetchCache.TryGetAsync(source.Url, cancellationToken);
            var useOcds = IETendersOcdsFetcher.IsETendersSource(source);
            if (cached is not null
                && source.ParserKind is not TenderParserKind.BrowserRendered
                && !useOcds)
            {
                body = cached;
                contentType = "text/html";
                httpStatus = 200;
                log.Message = "Served from short-TTL fetch cache";
            }
            else if (useOcds)
            {
                var page = await eTendersOcdsFetcher.FetchAsync(source, cancellationToken);
                httpStatus = page.StatusCode;
                body = page.Body;
                contentType = page.ContentType;
                source.LastFetchedAt = DateTimeOffset.UtcNow;
                log.Message = "Fetched from eTenders OCDS API";
            }
            else
            {
                var page = await pageFetcher.FetchHtmlAsync(source, source.Url, cancellationToken)
                    ?? throw new InvalidOperationException("Page fetch returned no content.");

                httpStatus = page.StatusCode;
                source.LastFetchedAt = DateTimeOffset.UtcNow;

                if (page.StatusCode == (int)HttpStatusCode.NotModified)
                {
                    run.SourcesUnchanged++;
                    log.Status = "Unchanged";
                    log.HttpStatus = httpStatus;
                    log.Message = "HTTP 304 Not Modified";
                    source.LastSuccessAt = DateTimeOffset.UtcNow;
                    source.LastError = null;
                    return createdMatches;
                }

                if (page.StatusCode is 429 or 503)
                {
                    OpenCircuit(source, TimeSpan.FromMinutes(options.Value.CircuitOpenMinutes), $"HTTP {page.StatusCode}");
                    log.Status = "Backoff";
                    log.HttpStatus = httpStatus;
                    log.Message = $"Backoff until {source.CircuitOpenedUntil:u}";
                    throw new InvalidOperationException($"Source returned HTTP {page.StatusCode}; circuit opened.");
                }

                if (page.StatusCode is < 200 or >= 300)
                {
                    throw new InvalidOperationException($"Source returned HTTP {page.StatusCode}.");
                }

                body = page.Body;
                contentType = page.ContentType;
                source.ETag = page.ETag ?? source.ETag;
                source.LastModifiedHeader = page.LastModified ?? source.LastModifiedHeader;
                if (source.ParserKind is not TenderParserKind.BrowserRendered)
                {
                    await fetchCache.SetAsync(source.Url, body, cancellationToken);
                }
            }

            log.HttpStatus = httpStatus;
            log.BytesFetched = body.Length;

            var contentHash = TenderUrlNormalizer.Sha256Hex(body);
            if (!string.IsNullOrWhiteSpace(source.ContentHash)
                && string.Equals(source.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase)
                && cached is null)
            {
                run.SourcesUnchanged++;
                log.Status = "Unchanged";
                log.Message = "Content hash unchanged";
                source.LastSuccessAt = DateTimeOffset.UtcNow;
                source.LastError = null;
                return createdMatches;
            }

            source.ContentHash = contentHash;

            var parser = parserRegistry.Resolve(source.ParserKind, contentType, body);
            var candidates = parser.ExtractCandidates(body, sourceUri);
            log.ItemsSeen = candidates.Count;

            var applicableQueries = queries
                .Where(query => query.SourceIds.Length == 0 || query.SourceIds.Contains(source.Id))
                .ToList();

            var knownSet = knownKeyCache.GetOrCreate(source.Id);
            if (knownSet.IsEmpty)
            {
                var knownKeys = await dbContext.TenderMatches
                    .Where(match => match.SourceId == source.Id)
                    .Select(match => match.ExternalKey)
                    .ToListAsync(cancellationToken);
                foreach (var key in knownKeys)
                {
                    knownSet.TryAdd(key, 0);
                }
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var detailBudget = source.MaxDetailPagesPerRun;
            var perSourceIndex = applicableQueries.Count == queries.Count
                ? keywordIndex
                : KeywordMatcher.Compile(
                    applicableQueries.Select(q => (q.Keywords, q.MatchMode)),
                    options.Value.AhoCorasickKeywordThreshold);

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (knownSet.ContainsKey(candidate.ExternalKey))
                {
                    log.ItemsSkippedKnown++;
                    run.ItemsSkippedKnown++;
                    continue;
                }

                if (IsExpired(candidate, today))
                {
                    log.ItemsSkippedExpired++;
                    run.ItemsSkippedExpired++;
                    continue;
                }

                var haystack = $"{candidate.Title} {candidate.Summary}";
                if (!KeywordMatcher.MatchesCompiled(haystack, perSourceIndex, out var matchedKeywords))
                {
                    continue;
                }

                log.ItemsMatched++;
                var documentUrls = candidate.DocumentUrls.ToList();

                if (!useOcds
                    && documentUrls.Count == 0
                    && detailBudget > 0
                    && !string.Equals(candidate.CanonicalUrl, source.Url, StringComparison.OrdinalIgnoreCase))
                {
                    detailBudget--;
                    try
                    {
                        var detailDocs = await FetchDetailDocumentUrlsAsync(source, candidate.CanonicalUrl, cancellationToken);
                        documentUrls.AddRange(detailDocs);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogDebug(ex, "Detail fetch failed for {Url}", candidate.CanonicalUrl);
                    }
                }

                var distinctDocs = documentUrls.Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToArray();
                string? docMetaJson = null;
                // OCDS payloads already include document title/format/url — skip slow HEAD probes.
                if (!useOcds
                    && options.Value.FetchDocumentMetadata
                    && distinctDocs.Length > 0)
                {
                    try
                    {
                        docMetaJson = TenderDocumentMetadata.Serialize(
                            await ProbeDocumentMetadataAsync(source, distinctDocs, cancellationToken));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogDebug(ex, "Document metadata probe failed for new match");
                    }
                }

                var collectedAt = DateTimeOffset.UtcNow;
                var match = new TenderMatch
                {
                    Id = Guid.NewGuid(),
                    SourceId = source.Id,
                    ExternalKey = Truncate(candidate.ExternalKey, 900)!,
                    CanonicalUrl = Truncate(candidate.CanonicalUrl, 1900)!,
                    Title = Truncate(candidate.Title, 900)!,
                    Summary = Truncate(candidate.Summary, 3900),
                    ClosingDate = candidate.ClosingDate,
                    PortalStatus = candidate.PortalStatus,
                    MatchedKeywords = matchedKeywords,
                    DocumentUrls = distinctDocs,
                    DocumentMetadataJson = docMetaJson,
                    ContentHash = TenderUrlNormalizer.Sha256Hex(
                        $"{candidate.Title}|{string.Join('|', distinctDocs)}"),
                    FirstSeenAt = collectedAt,
                    CollectedAt = collectedAt,
                    Status = TenderMatchStatus.New,
                    OwnerUserId = run.RequestedByUserId,
                };

                dbContext.TenderMatches.Add(match);
                knownSet.TryAdd(match.ExternalKey, 0);
                knownKeyCache.Remember(source.Id, match.ExternalKey);
                run.MatchesNew++;
                createdMatches.Add(match);
            }

            log.Status = "Succeeded";
            log.Message = string.IsNullOrWhiteSpace(log.Message)
                ? $"Parsed with {parser.GetType().Name}"
                : $"{log.Message}; parsed with {parser.GetType().Name}";
            RecordSuccess(source);
            source.LastSuccessAt = DateTimeOffset.UtcNow;
            source.LastError = null;
            return createdMatches;
        }
        finally
        {
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            if (source.ScrapeIntervalMinutes is > 0)
            {
                ScheduleNextDue(source);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private void RecordSuccess(TenderSource source)
    {
        source.ConsecutiveFailures = 0;
        source.CircuitOpenedUntil = null;
        source.LastError = null;
    }

    private void RecordFailure(TenderSource source, string message)
    {
        source.LastError = message;
        source.LastFetchedAt = DateTimeOffset.UtcNow;
        source.ConsecutiveFailures++;
        knownKeyCache.Invalidate(source.Id);

        if (source.ConsecutiveFailures >= Math.Max(1, options.Value.CircuitFailureThreshold))
        {
            OpenCircuit(source, TimeSpan.FromMinutes(Math.Max(1, options.Value.CircuitOpenMinutes)), message);
        }
    }

    private void OpenCircuit(TenderSource source, TimeSpan openFor, string reason)
    {
        source.CircuitOpenedUntil = DateTimeOffset.UtcNow + openFor;
        source.LastError = Truncate($"Circuit open: {reason}", 1900);
        logger.LogWarning(
            "Opened tender scrape circuit for {SourceId} until {Until}",
            source.Id,
            source.CircuitOpenedUntil);
    }

    private async Task NotifyNewMatchesAsync(
        IReadOnlyList<(TenderMatch Match, TenderSource Source)> newMatches,
        CancellationToken cancellationToken)
    {
        if (newMatches.Count == 0)
        {
            return;
        }

        var subscriptions = await dbContext.TenderWatchSubscriptions
            .AsNoTracking()
            .Where(sub => sub.NotifyInApp)
            .ToListAsync(cancellationToken);

        var queryLookup = await dbContext.TenderQueries
            .AsNoTracking()
            .ToDictionaryAsync(query => query.Id, cancellationToken);

        foreach (var (match, source) in newMatches)
        {
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(match.OwnerUserId))
            {
                targets.Add(match.OwnerUserId);
            }

            foreach (var sub in subscriptions)
            {
                if (sub.SourceId is not null && sub.SourceId != source.Id)
                {
                    continue;
                }

                if (sub.QueryId is not null)
                {
                    if (!queryLookup.TryGetValue(sub.QueryId.Value, out var query))
                    {
                        continue;
                    }

                    var overlap = match.MatchedKeywords
                        .Intersect(query.Keywords, StringComparer.OrdinalIgnoreCase)
                        .Any();
                    if (!overlap)
                    {
                        continue;
                    }
                }

                targets.Add(sub.UserId);
            }

            foreach (var userId in targets)
            {
                await eventBus.PublishAsync(
                    new TenderMatchFoundEvent(
                        match.Id,
                        match.Title,
                        source.Name,
                        match.CanonicalUrl,
                        userId,
                        DateTimeOffset.UtcNow),
                    cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<string>> FetchDetailDocumentUrlsAsync(
        TenderSource source,
        string url,
        CancellationToken cancellationToken)
    {
        if (source.ParserKind is not TenderParserKind.BrowserRendered)
        {
            var cached = await fetchCache.TryGetAsync(url, cancellationToken);
            if (cached is not null)
            {
                return HtmlTenderParser.ExtractDocumentUrls(cached, new Uri(url));
            }
        }

        await using var _ = await concurrencyGate.AcquireAsync(new Uri(url), cancellationToken);
        var page = await pageFetcher.FetchHtmlAsync(source, url, cancellationToken);
        if (page is null || page.StatusCode is < 200 or >= 300)
        {
            return [];
        }

        if (source.ParserKind is not TenderParserKind.BrowserRendered)
        {
            await fetchCache.SetAsync(url, page.Body, cancellationToken);
        }

        return HtmlTenderParser.ExtractDocumentUrls(page.Body, new Uri(url));
    }

    private async Task<IReadOnlyList<TenderDocumentMeta>> ProbeDocumentMetadataAsync(
        TenderSource source,
        IReadOnlyList<string> urls,
        CancellationToken cancellationToken)
    {
        var limit = Math.Max(1, options.Value.MaxDocumentMetadataPerMatch);
        var client = httpClientFactory.CreateClient(HttpClientName);
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
                    using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpTenderPageFetcher.ApplyAuth(getRequest, source, secrets);
                    getRequest.Headers.Range = new RangeHeaderValue(0, 0);
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
            ?? response.Content.Headers.ContentDisposition?.FileName;
        if (fileName is not null)
        {
            fileName = fileName.Trim('"');
        }
        else
        {
            try
            {
                var name = Path.GetFileName(new Uri(url).AbsolutePath);
                fileName = string.IsNullOrWhiteSpace(name) ? null : Uri.UnescapeDataString(name);
            }
            catch
            {
                fileName = null;
            }
        }

        return new TenderDocumentMeta(url, contentType, fileName, response.Content.Headers.ContentLength);
    }

    private async Task<TenderScrapeRunDto?> MapRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var run = await dbContext.TenderScrapeRuns
            .AsNoTracking()
            .Include(item => item.SourceLogs)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (run is null)
        {
            return null;
        }

        var names = await LoadSourceNamesAsync(run.SourceLogs.Select(l => l.SourceId), cancellationToken);
        return TenderMapper.ToDto(run, names);
    }

    private async Task<Dictionary<Guid, string>> LoadSourceNamesAsync(
        IEnumerable<Guid> sourceIds,
        CancellationToken cancellationToken)
    {
        var ids = sourceIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await dbContext.TenderSources
            .AsNoTracking()
            .Where(source => ids.Contains(source.Id))
            .ToDictionaryAsync(source => source.Id, source => source.Name, cancellationToken);
    }

    internal static void ScheduleNextDue(TenderSource source)
    {
        if (source.ScrapeIntervalMinutes is not > 0)
        {
            source.NextDueAt = null;
            return;
        }

        var interval = TimeSpan.FromMinutes(source.ScrapeIntervalMinutes.Value);
        var jitter = TimeSpan.FromMilliseconds(Jitter.NextDouble() * interval.TotalMilliseconds * 0.2);
        source.NextDueAt = DateTimeOffset.UtcNow + interval + jitter;
    }

    private static bool IsExpired(TenderCandidate candidate, DateOnly today)
    {
        if (candidate.PortalStatus is TenderPortalStatus.Closed
            or TenderPortalStatus.Awarded
            or TenderPortalStatus.Cancelled
            or TenderPortalStatus.Expired)
        {
            return true;
        }

        return candidate.ClosingDate is not null && candidate.ClosingDate.Value < today;
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
