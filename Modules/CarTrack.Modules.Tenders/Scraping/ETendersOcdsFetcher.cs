using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Tenders;

/// <summary>
/// Fetches paginated OCDS releases from the National Treasury eTenders API.
/// Requires <c>dateFrom</c>/<c>dateTo</c> (published/closing window).
/// </summary>
public interface IETendersOcdsFetcher
{
    Task<TenderFetchedPage> FetchAsync(
        TenderSource source,
        CancellationToken cancellationToken = default);

    static bool IsETendersSource(TenderSource source)
    {
        if (source.ParserKind is TenderParserKind.ETenders)
        {
            return true;
        }

        if (source.ParserKind is not TenderParserKind.Auto)
        {
            return false;
        }

        return Uri.TryCreate(source.Url, UriKind.Absolute, out var uri)
            && uri.Host.Contains("etenders.gov.za", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ETendersOcdsFetcher(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TenderScrapeOptions> options,
    ILogger<ETendersOcdsFetcher> logger) : IETendersOcdsFetcher
{
    public const string HttpClientName = "ETendersOcds";

    public async Task<TenderFetchedPage> FetchAsync(
        TenderSource source,
        CancellationToken cancellationToken = default)
    {
        var scrape = options.CurrentValue;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // dateFrom = published lookback; dateTo must be later than today (open closings).
        var dateFrom = today.AddDays(-Math.Max(1, scrape.ETendersLookbackDays));
        var dateTo = today.AddDays(Math.Max(1, scrape.ETendersForwardDays));
        var pageSize = Math.Clamp(scrape.ETendersPageSize, 1, 100);
        var maxPages = Math.Clamp(scrape.ETendersMaxPages, 1, 200);

        var client = httpClientFactory.CreateClient(HttpClientName);
        var releaseJsonChunks = new List<string>();
        var nextUrl = BuildFirstPageUrl(scrape.ETendersApiBaseUrl, dateFrom, dateTo, pageSize);
        var pagesFetched = 0;
        var lastStatus = (int)HttpStatusCode.OK;
        var fetchWatch = Stopwatch.StartNew();

        // Aspire/OTel HttpClient logs redact query strings as "?*" — log the real URL here.
        logger.LogInformation(
            "eTenders OCDS request URL: {RequestUrl} (source {SourceId})",
            nextUrl,
            source.Id);

        while (!string.IsNullOrWhiteSpace(nextUrl) && pagesFetched < maxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pagesFetched++;
            var pageWatch = Stopwatch.StartNew();

            logger.LogInformation(
                "eTenders OCDS GET page {Page}: {RequestUrl}",
                pagesFetched,
                nextUrl);

            using var response = await client.GetAsync(nextUrl, cancellationToken);
            lastStatus = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"eTenders OCDS API returned HTTP {lastStatus} for {nextUrl}: {Truncate(body, 400)}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var pageCount = 0;
            if (root.TryGetProperty("releases", out var releases) && releases.ValueKind == JsonValueKind.Array)
            {
                foreach (var release in releases.EnumerateArray())
                {
                    releaseJsonChunks.Add(release.GetRawText());
                    pageCount++;
                }
            }

            nextUrl = null;
            if (root.TryGetProperty("links", out var links)
                && links.ValueKind == JsonValueKind.Object
                && links.TryGetProperty("next", out var next)
                && next.ValueKind == JsonValueKind.String)
            {
                var candidate = next.GetString();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    nextUrl = candidate;
                }
            }

            logger.LogInformation(
                "eTenders OCDS page {Page} returned {Count} releases in {ElapsedMs}ms (more={HasMore})",
                pagesFetched,
                pageCount,
                pageWatch.ElapsedMilliseconds,
                !string.IsNullOrWhiteSpace(nextUrl));

            if (pageCount == 0)
            {
                break;
            }
        }

        logger.LogInformation(
            "Fetched {Count} eTenders OCDS releases across {Pages} page(s) for {DateFrom}..{DateTo} in {ElapsedMs}ms (source {SourceId})",
            releaseJsonChunks.Count,
            pagesFetched,
            dateFrom,
            dateTo,
            fetchWatch.ElapsedMilliseconds,
            source.Id);

        var combined = BuildPackageJson(
            BuildFirstPageUrl(scrape.ETendersApiBaseUrl, dateFrom, dateTo, pageSize),
            releaseJsonChunks);

        return new TenderFetchedPage(
            combined,
            "application/json",
            lastStatus,
            ETag: null,
            LastModified: null);
    }

    private static string BuildPackageJson(string uri, IReadOnlyList<string> releaseJsonChunks)
    {
        var sb = new StringBuilder(Math.Max(256, releaseJsonChunks.Sum(chunk => chunk.Length) + 128));
        sb.Append("{\"uri\":");
        sb.Append(JsonSerializer.Serialize(uri));
        sb.Append(",\"version\":\"1.1\",\"publishedDate\":");
        sb.Append(JsonSerializer.Serialize(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)));
        sb.Append(",\"releases\":[");
        for (var i = 0; i < releaseJsonChunks.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(releaseJsonChunks[i]);
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private static string BuildFirstPageUrl(string apiBaseUrl, DateOnly dateFrom, DateOnly dateTo, int pageSize)
    {
        var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
            ? "https://ocds-api.etenders.gov.za/api/OCDSReleases"
            : apiBaseUrl.TrimEnd('/');

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{baseUrl}?PageNumber=1&PageSize={pageSize}&dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
