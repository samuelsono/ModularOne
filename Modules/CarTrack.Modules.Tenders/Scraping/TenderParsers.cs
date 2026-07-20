using System.Globalization;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;

namespace CarTrack.Modules.Tenders;

public interface ITenderSourceParser
{
    bool CanParse(TenderParserKind kind, string contentType, string body);

    IReadOnlyList<TenderCandidate> ExtractCandidates(string body, Uri sourceUri);
}

public sealed class RssAtomTenderParser : ITenderSourceParser
{
    public bool CanParse(TenderParserKind kind, string contentType, string body)
    {
        if (kind is TenderParserKind.RssAtom)
        {
            return true;
        }

        if (kind is not TenderParserKind.Auto)
        {
            return false;
        }

        var ct = contentType.ToLowerInvariant();
        if (ct.Contains("rss") || ct.Contains("atom") || ct.Contains("xml"))
        {
            return true;
        }

        var trimmed = body.TrimStart();
        return trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("<rss", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("<feed", StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TenderCandidate> ExtractCandidates(string body, Uri sourceUri)
    {
        using var reader = XmlReader.Create(new StringReader(body));
        var feed = SyndicationFeed.Load(reader);
        var results = new List<TenderCandidate>();

        foreach (var item in feed.Items)
        {
            var link = item.Links.FirstOrDefault()?.Uri?.AbsoluteUri
                ?? item.Id
                ?? string.Empty;
            if (string.IsNullOrWhiteSpace(link))
            {
                continue;
            }

            string canonical;
            try
            {
                canonical = TenderUrlNormalizer.Canonicalize(link, sourceUri);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var title = item.Title?.Text?.Trim() ?? canonical;
            var summary = item.Summary?.Text?.Trim()
                ?? item.Content?.ToString();
            var closing = TryParseClosingDate($"{title} {summary}");
            var docs = ExtractDocumentUrls($"{summary}", sourceUri);

            results.Add(new TenderCandidate(
                ExternalKey: item.Id ?? canonical,
                CanonicalUrl: canonical,
                Title: title,
                Summary: Truncate(summary, 3900),
                ClosingDate: closing,
                PortalStatus: InferPortalStatus(closing, $"{title} {summary}"),
                DocumentUrls: docs));
        }

        return results;
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    private static DateOnly? TryParseClosingDate(string text) =>
        HtmlTenderParser.TryParseClosingDate(text);

    private static TenderPortalStatus InferPortalStatus(DateOnly? closing, string text) =>
        HtmlTenderParser.InferPortalStatus(closing, text);

    private static IReadOnlyList<string> ExtractDocumentUrls(string text, Uri baseUri) =>
        HtmlTenderParser.ExtractDocumentUrls(text, baseUri);
}

public sealed partial class HtmlTenderParser : ITenderSourceParser
{
    private static readonly Regex AnchorRegex = AnchorPattern();
    private static readonly Regex ClosingDateRegex = ClosingDatePattern();
    private static readonly Regex ClosedStatusRegex = ClosedStatusPattern();
    private static readonly string[] DocumentExtensions =
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar"];

    public bool CanParse(TenderParserKind kind, string contentType, string body)
    {
        if (kind is TenderParserKind.GenericHtml or TenderParserKind.BrowserRendered)
        {
            return true;
        }

        if (kind is not TenderParserKind.Auto)
        {
            return false;
        }

        var ct = contentType.ToLowerInvariant();
        return ct.Contains("html") || body.Contains("<a ", StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TenderCandidate> ExtractCandidates(string body, Uri sourceUri)
    {
        var results = new List<TenderCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AnchorRegex.Matches(body))
        {
            var href = System.Net.WebUtility.HtmlDecode(match.Groups["href"].Value).Trim();
            var text = System.Net.WebUtility.HtmlDecode(StripTags(match.Groups["text"].Value)).Trim();
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#') || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string canonical;
            try
            {
                canonical = TenderUrlNormalizer.Canonicalize(href, sourceUri);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (!seen.Add(canonical))
            {
                continue;
            }

            // Skip pure document anchors as top-level tenders; they become DocumentUrls on parents.
            if (IsDocumentUrl(canonical))
            {
                continue;
            }

            var title = string.IsNullOrWhiteSpace(text) ? canonical : text;
            if (title.Length < 8)
            {
                continue;
            }

            var contextStart = Math.Max(0, match.Index - 240);
            var contextLength = Math.Min(body.Length - contextStart, match.Length + 480);
            var context = StripTags(System.Net.WebUtility.HtmlDecode(body.Substring(contextStart, contextLength)));
            var closing = TryParseClosingDate($"{title} {context}");
            var docs = ExtractDocumentUrls(context, sourceUri)
                .Concat(ExtractNearbyDocumentUrls(body, match.Index, sourceUri))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToArray();

            results.Add(new TenderCandidate(
                ExternalKey: canonical,
                CanonicalUrl: canonical,
                Title: Truncate(title, 900)!,
                Summary: Truncate(context, 3900),
                ClosingDate: closing,
                PortalStatus: InferPortalStatus(closing, $"{title} {context}"),
                DocumentUrls: docs));
        }

        return results;
    }

    internal static DateOnly? TryParseClosingDate(string text)
    {
        foreach (Match match in ClosingDateRegex.Matches(text))
        {
            var raw = match.Groups["date"].Value.Trim();
            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || DateOnly.TryParse(raw, CultureInfo.GetCultureInfo("en-ZA"), DateTimeStyles.None, out date)
                || DateOnly.TryParseExact(raw, ["yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "d MMM yyyy", "d MMMM yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date;
            }
        }

        return null;
    }

    internal static TenderPortalStatus InferPortalStatus(DateOnly? closing, string text)
    {
        if (ClosedStatusRegex.IsMatch(text))
        {
            return TenderPortalStatus.Closed;
        }

        if (closing is not null && closing.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return TenderPortalStatus.Expired;
        }

        return closing is null ? TenderPortalStatus.Unknown : TenderPortalStatus.Open;
    }

    internal static IReadOnlyList<string> ExtractDocumentUrls(string text, Uri baseUri)
    {
        var urls = new List<string>();
        foreach (Match match in AnchorRegex.Matches(text))
        {
            var href = System.Net.WebUtility.HtmlDecode(match.Groups["href"].Value).Trim();
            TryAddDocument(href, baseUri, urls);
        }

        foreach (Match match in UrlInTextPattern().Matches(text))
        {
            TryAddDocument(match.Value, baseUri, urls);
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IEnumerable<string> ExtractNearbyDocumentUrls(string body, int index, Uri baseUri)
    {
        var start = Math.Max(0, index - 600);
        var length = Math.Min(body.Length - start, 1400);
        return ExtractDocumentUrls(body.Substring(start, length), baseUri);
    }

    private static void TryAddDocument(string href, Uri baseUri, List<string> urls)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return;
        }

        try
        {
            var canonical = TenderUrlNormalizer.Canonicalize(href, baseUri);
            if (IsDocumentUrl(canonical))
            {
                urls.Add(canonical);
            }
        }
        catch (InvalidOperationException)
        {
            // ignore
        }
    }

    private static bool IsDocumentUrl(string url)
    {
        var path = url.Split('?', 2)[0];
        return DocumentExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private static string StripTags(string html) => TagPattern().Replace(html, " ");

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    [GeneratedRegex("""<a\s+[^>]*href\s*=\s*["'](?<href>[^"']+)["'][^>]*>(?<text>.*?)</a>""", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex AnchorPattern();

    [GeneratedRegex("""(?i)(closing|close|deadline|submission|due)\s*(date)?\s*[:\-]?\s*(?<date>\d{1,4}[\-/\s]\w{0,9}[\-/\s]\d{1,4}|\d{1,2}\s+\w+\s+\d{4})""")]
    private static partial Regex ClosingDatePattern();

    [GeneratedRegex("""(?i)\b(closed|expired|cancelled|canceled|awarded)\b""")]
    private static partial Regex ClosedStatusPattern();

    [GeneratedRegex("""https?://[^\s"'<>]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlInTextPattern();

    [GeneratedRegex("""<[^>]+>""", RegexOptions.Compiled)]
    private static partial Regex TagPattern();
}

public sealed class TenderParserRegistry(IEnumerable<ITenderSourceParser> parsers)
{
    private readonly IReadOnlyList<ITenderSourceParser> _parsers = parsers.ToList();

    public ITenderSourceParser Resolve(TenderParserKind kind, string contentType, string body)
    {
        var match = _parsers.FirstOrDefault(parser => parser.CanParse(kind, contentType, body));
        return match ?? _parsers.OfType<HtmlTenderParser>().First();
    }
}
