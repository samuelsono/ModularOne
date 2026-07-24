using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CarTrack.Modules.Tenders;

internal static class TenderUrlNormalizer
{
    private static readonly HashSet<string> TrackingParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "utm_id",
        "fbclid", "gclid", "mc_cid", "mc_eid", "msclkid", "_ga", "_gl",
        "sessionid", "session_id", "sid", "phpsessid", "jsessionid", "asp.net_sessionid",
        "cfid", "cftoken",
    };

    public static string Canonicalize(string url, Uri? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("URL is required.");
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var absolute)
            && baseUri is not null
            && Uri.TryCreate(baseUri, url.Trim(), out var relative))
        {
            absolute = relative;
        }

        if (absolute is null || (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("URL must be an absolute http(s) address.");
        }

        var builder = new UriBuilder(absolute)
        {
            Fragment = string.Empty,
        };

        if ((builder.Scheme == "http" && builder.Port == 80)
            || (builder.Scheme == "https" && builder.Port == 443))
        {
            builder.Port = -1;
        }

        // Drop ;jsessionid=... path suffixes common on Java portals.
        var path = builder.Path;
        var semi = path.IndexOf(';');
        if (semi >= 0)
        {
            builder.Path = path[..semi];
        }

        if (!string.IsNullOrEmpty(builder.Query))
        {
            var pairs = builder.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length > 0 && !TrackingParams.Contains(Uri.UnescapeDataString(parts[0])))
                .OrderBy(parts => parts[0], StringComparer.OrdinalIgnoreCase)
                .Select(parts => parts.Length == 1
                    ? parts[0]
                    : $"{parts[0]}={parts[1]}");

            builder.Query = string.Join('&', pairs);
        }

        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }

    public static string Sha256Hex(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

internal static partial class KeywordMatcher
{
    public static string[] NormalizeKeywords(IEnumerable<string?> keywords)
    {
        return keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            // Split pasted "software, develop, SAP" into separate terms.
            .SelectMany(keyword => keyword!.Split([',', ';', '|', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(keyword => Regex.Replace(keyword.Trim().ToLowerInvariant(), @"\s+", " "))
            .Where(keyword => keyword.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static CompiledKeywordIndex Compile(
        IEnumerable<(string[] Keywords, TenderQueryMatchMode Mode)> queries,
        int ahoThreshold)
    {
        var anyKeywords = new HashSet<string>(StringComparer.Ordinal);
        var allGroups = new List<string[]>();
        var phrases = new List<string>();

        foreach (var (rawKeywords, mode) in queries)
        {
            var keywords = NormalizeKeywords(rawKeywords);
            if (keywords.Length == 0)
            {
                continue;
            }

            switch (mode)
            {
                case TenderQueryMatchMode.All:
                    allGroups.Add(keywords);
                    break;
                case TenderQueryMatchMode.Phrase:
                    phrases.Add(string.Join(' ', keywords));
                    break;
                default:
                    foreach (var keyword in keywords)
                    {
                        anyKeywords.Add(keyword);
                    }

                    break;
            }
        }

        AhoCorasickMatcher? aho = null;
        if (anyKeywords.Count >= ahoThreshold)
        {
            aho = new AhoCorasickMatcher(anyKeywords);
        }

        return new CompiledKeywordIndex(anyKeywords.ToArray(), allGroups, phrases, aho);
    }

    public static bool MatchesCompiled(
        string haystack,
        CompiledKeywordIndex index,
        out string[] matched)
    {
        matched = [];
        if (string.IsNullOrWhiteSpace(haystack))
        {
            return false;
        }

        var text = haystack.ToLowerInvariant();
        var hits = new HashSet<string>(StringComparer.Ordinal);

        if (index.Aho is not null)
        {
            foreach (var hit in index.Aho.FindAll(text))
            {
                hits.Add(hit);
            }
        }
        else
        {
            foreach (var keyword in index.AnyKeywords)
            {
                if (text.Contains(keyword))
                {
                    hits.Add(keyword);
                }
            }
        }

        foreach (var group in index.AllGroups)
        {
            if (group.All(text.Contains))
            {
                foreach (var keyword in group)
                {
                    hits.Add(keyword);
                }
            }
        }

        foreach (var phrase in index.Phrases)
        {
            if (text.Contains(phrase))
            {
                hits.Add(phrase);
            }
        }

        matched = hits.ToArray();
        return matched.Length > 0;
    }

    public static bool Matches(
        string haystack,
        string[] keywords,
        TenderQueryMatchMode mode,
        out string[] matched)
    {
        matched = [];
        if (keywords.Length == 0 || string.IsNullOrWhiteSpace(haystack))
        {
            return false;
        }

        var text = haystack.ToLowerInvariant();
        switch (mode)
        {
            case TenderQueryMatchMode.All:
                if (keywords.All(text.Contains))
                {
                    matched = keywords;
                    return true;
                }

                return false;

            case TenderQueryMatchMode.Phrase:
                var phrase = string.Join(' ', keywords);
                if (text.Contains(phrase))
                {
                    matched = [phrase];
                    return true;
                }

                return false;

            default:
                matched = keywords.Where(text.Contains).ToArray();
                return matched.Length > 0;
        }
    }
}

internal sealed record CompiledKeywordIndex(
    string[] AnyKeywords,
    IReadOnlyList<string[]> AllGroups,
    IReadOnlyList<string> Phrases,
    AhoCorasickMatcher? Aho);

public sealed record TenderCandidate(
    string ExternalKey,
    string CanonicalUrl,
    string Title,
    string? Summary,
    DateOnly? ClosingDate,
    TenderPortalStatus PortalStatus,
    IReadOnlyList<string> DocumentUrls);
