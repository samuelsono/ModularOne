using System.Globalization;
using System.Text.RegularExpressions;

namespace CarTrack.Modules.Tenders;

/// <summary>
/// Adapter for South African eTenders / National Treasury style listing HTML.
/// Prefer this over GenericHtml when the portal uses tender-number rows and closing-date columns.
/// </summary>
public sealed partial class ETendersTenderParser : ITenderSourceParser
{
    private static readonly Regex TenderNumberRegex = TenderNumberPattern();
    private static readonly Regex RowishRegex = RowishPattern();

    public bool CanParse(TenderParserKind kind, string contentType, string body)
    {
        if (kind is TenderParserKind.ETenders)
        {
            return true;
        }

        if (kind is not TenderParserKind.Auto)
        {
            return false;
        }

        var lower = body.ToLowerInvariant();
        return lower.Contains("etenders")
            || lower.Contains("national treasury")
            || lower.Contains("tender number")
            || TenderNumberRegex.IsMatch(body);
    }

    public IReadOnlyList<TenderCandidate> ExtractCandidates(string body, Uri sourceUri)
    {
        var fromRows = ExtractFromRows(body, sourceUri);
        if (fromRows.Count > 0)
        {
            return fromRows;
        }

        // Fall back to generic HTML anchors when the page is a simple link list.
        return new HtmlTenderParser().ExtractCandidates(body, sourceUri);
    }

    private static IReadOnlyList<TenderCandidate> ExtractFromRows(string body, Uri sourceUri)
    {
        var results = new List<TenderCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match row in RowishRegex.Matches(body))
        {
            var chunk = System.Net.WebUtility.HtmlDecode(StripTags(row.Value));
            var tenderNo = TenderNumberRegex.Match(chunk);
            if (!tenderNo.Success)
            {
                continue;
            }

            var number = tenderNo.Groups["num"].Value.Trim();
            var hrefMatch = AnchorInChunkPattern().Match(row.Value);
            string canonical;
            try
            {
                if (hrefMatch.Success)
                {
                    canonical = TenderUrlNormalizer.Canonicalize(
                        System.Net.WebUtility.HtmlDecode(hrefMatch.Groups["href"].Value),
                        sourceUri);
                }
                else
                {
                    canonical = TenderUrlNormalizer.Canonicalize(
                        sourceUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "#" + Uri.EscapeDataString(number),
                        sourceUri);
                }
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var externalKey = number;
            if (!seen.Add(externalKey))
            {
                continue;
            }

            var title = ExtractTitle(chunk, number);
            var closing = HtmlTenderParser.TryParseClosingDate(chunk);
            var docs = HtmlTenderParser.ExtractDocumentUrls(row.Value, sourceUri);

            results.Add(new TenderCandidate(
                ExternalKey: externalKey,
                CanonicalUrl: canonical,
                Title: Truncate(title, 900)!,
                Summary: Truncate(chunk, 3900),
                ClosingDate: closing,
                PortalStatus: HtmlTenderParser.InferPortalStatus(closing, chunk),
                DocumentUrls: docs.ToArray()));
        }

        return results;
    }

    private static string ExtractTitle(string chunk, string tenderNumber)
    {
        var withoutNumber = chunk.Replace(tenderNumber, " ", StringComparison.OrdinalIgnoreCase);
        var cleaned = WhitespacePattern().Replace(withoutNumber, " ").Trim();
        if (cleaned.Length < 12)
        {
            return $"Tender {tenderNumber}";
        }

        return cleaned.Length > 180 ? cleaned[..180].Trim() : cleaned;
    }

    private static string StripTags(string html) => TagPattern().Replace(html, " ");

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    [GeneratedRegex("""(?i)\b(?<num>(RFQ|RFI|RFP|BID|TENDER)?[\s\-]?\d{2,}[A-Z0-9\-\/]{2,})\b""")]
    private static partial Regex TenderNumberPattern();

    [GeneratedRegex("""<(tr|li|article|div)[^>]{0,200}>(?<inner>.*?)</\1>""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RowishPattern();

    [GeneratedRegex("""<a\s+[^>]*href\s*=\s*["'](?<href>[^"']+)["'][^>]*>""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorInChunkPattern();

    [GeneratedRegex("""\s+""", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("""<[^>]+>""", RegexOptions.Compiled)]
    private static partial Regex TagPattern();
}
