using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Tenders;

public interface ITenderPageFetcher
{
    Task<TenderFetchedPage?> FetchHtmlAsync(
        TenderSource source,
        string url,
        CancellationToken cancellationToken = default);
}

public sealed record TenderFetchedPage(
    string Body,
    string ContentType,
    int StatusCode,
    string? ETag,
    string? LastModified);

/// <summary>HttpClient fetch with optional Basic / Bearer / Cookie auth from the source.</summary>
public sealed class HttpTenderPageFetcher(
    IHttpClientFactory httpClientFactory,
    ITenderSourceSecretProtector secrets) : ITenderPageFetcher
{
    public async Task<TenderFetchedPage?> FetchHtmlAsync(
        TenderSource source,
        string url,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(TenderScrapeService.HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuth(request, source, secrets);

        if (!string.IsNullOrWhiteSpace(source.ETag)
            && string.Equals(url, source.Url, StringComparison.OrdinalIgnoreCase)
            && EntityTagHeaderValue.TryParse(source.ETag, out var etag))
        {
            request.Headers.IfNoneMatch.Add(etag);
        }

        if (!string.IsNullOrWhiteSpace(source.LastModifiedHeader)
            && string.Equals(url, source.Url, StringComparison.OrdinalIgnoreCase)
            && DateTimeOffset.TryParse(source.LastModifiedHeader, out var lastModified))
        {
            request.Headers.IfModifiedSince = lastModified;
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var body = response.StatusCode == System.Net.HttpStatusCode.NotModified
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        return new TenderFetchedPage(
            body,
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            (int)response.StatusCode,
            response.Headers.ETag?.Tag,
            response.Content.Headers.LastModified?.ToString("R"));
    }

    internal static void ApplyAuth(
        HttpRequestMessage request,
        TenderSource source,
        ITenderSourceSecretProtector secrets)
    {
        if (source.AuthKind is TenderSourceAuthKind.None)
        {
            return;
        }

        var secret = secrets.Unprotect(source.ProtectedAuthSecret);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        switch (source.AuthKind)
        {
            case TenderSourceAuthKind.Basic:
            {
                var user = source.AuthUsername ?? string.Empty;
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{secret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
                break;
            }
            case TenderSourceAuthKind.Bearer:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
                break;
            case TenderSourceAuthKind.CookieHeader:
                request.Headers.TryAddWithoutValidation("Cookie", secret);
                break;
        }
    }
}

/// <summary>
/// Optional Playwright hook. Default build does not reference Microsoft.Playwright.
/// When <see cref="TenderScrapeOptions.EnablePlaywright"/> is true, falls back to HTTP
/// with a clear log, unless a custom <see cref="ITenderBrowserRenderer"/> is registered.
/// </summary>
public interface ITenderBrowserRenderer
{
    Task<TenderFetchedPage?> RenderAsync(
        TenderSource source,
        string url,
        IReadOnlyDictionary<string, string> extraHeaders,
        CancellationToken cancellationToken = default);
}

public sealed class NullTenderBrowserRenderer : ITenderBrowserRenderer
{
    public Task<TenderFetchedPage?> RenderAsync(
        TenderSource source,
        string url,
        IReadOnlyDictionary<string, string> extraHeaders,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TenderFetchedPage?>(null);
}

public sealed class PlaywrightTenderPageFetcher(
    IOptionsMonitor<TenderScrapeOptions> options,
    ITenderSourceSecretProtector secrets,
    ITenderBrowserRenderer browserRenderer,
    ILogger<PlaywrightTenderPageFetcher> logger) : ITenderPageFetcher
{
    public async Task<TenderFetchedPage?> FetchHtmlAsync(
        TenderSource source,
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!options.CurrentValue.EnablePlaywright)
        {
            throw new InvalidOperationException(
                "BrowserRendered requires Tenders:Scrape:EnablePlaywright=true and an ITenderBrowserRenderer (Playwright) registration.");
        }

        var headers = BuildExtraHeaders(source, secrets);
        var rendered = await browserRenderer.RenderAsync(source, url, headers, cancellationToken);
        if (rendered is not null)
        {
            return rendered;
        }

        logger.LogWarning(
            "No ITenderBrowserRenderer produced content for {Url}. Register a Playwright-backed renderer or use GenericHtml/ETenders.",
            url);
        throw new InvalidOperationException(
            "BrowserRendered is enabled but no Playwright renderer is registered. Add an ITenderBrowserRenderer implementation that uses Microsoft.Playwright.");
    }

    private static Dictionary<string, string> BuildExtraHeaders(
        TenderSource source,
        ITenderSourceSecretProtector secrets)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source.AuthKind is TenderSourceAuthKind.None)
        {
            return headers;
        }

        var secret = secrets.Unprotect(source.ProtectedAuthSecret);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return headers;
        }

        switch (source.AuthKind)
        {
            case TenderSourceAuthKind.Basic:
            {
                var user = source.AuthUsername ?? string.Empty;
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{secret}"));
                headers["Authorization"] = $"Basic {token}";
                break;
            }
            case TenderSourceAuthKind.Bearer:
                headers["Authorization"] = $"Bearer {secret}";
                break;
            case TenderSourceAuthKind.CookieHeader:
                headers["Cookie"] = secret;
                break;
        }

        return headers;
    }
}

public sealed class TenderPageFetchRouter(
    HttpTenderPageFetcher httpFetcher,
    PlaywrightTenderPageFetcher playwrightFetcher) : ITenderPageFetcher
{
    public Task<TenderFetchedPage?> FetchHtmlAsync(
        TenderSource source,
        string url,
        CancellationToken cancellationToken = default) =>
        source.ParserKind is TenderParserKind.BrowserRendered
            ? playwrightFetcher.FetchHtmlAsync(source, url, cancellationToken)
            : httpFetcher.FetchHtmlAsync(source, url, cancellationToken);
}
