using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarTrack.Modules.Tenders;

/// <summary>
/// Parses National Treasury eTenders OCDS release packages from
/// https://ocds-api.etenders.gov.za/api/OCDSReleases.
/// The public HTML portal is JS-rendered and is not a reliable listing source.
/// </summary>
public sealed class ETendersTenderParser : ITenderSourceParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

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

        var ct = contentType.ToLowerInvariant();
        if (ct.Contains("json") && LooksLikeOcdsPackage(body))
        {
            return true;
        }

        return LooksLikeOcdsPackage(body);
    }

    public IReadOnlyList<TenderCandidate> ExtractCandidates(string body, Uri sourceUri)
    {
        if (string.IsNullOrWhiteSpace(body) || !LooksLikeOcdsPackage(body))
        {
            return [];
        }

        OcdsReleasePackage? package;
        try
        {
            package = JsonSerializer.Deserialize<OcdsReleasePackage>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (package?.Releases is not { Count: > 0 })
        {
            return [];
        }

        var results = new List<TenderCandidate>(package.Releases.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var portalBase = ResolvePortalBase(sourceUri);

        foreach (var release in package.Releases)
        {
            var tender = release.Tender;
            if (tender is null)
            {
                continue;
            }

            var externalKey = FirstNonEmpty(release.Ocid, tender.Id, release.Id);
            if (string.IsNullOrWhiteSpace(externalKey) || !seen.Add(externalKey))
            {
                continue;
            }

            var titleCode = tender.Title?.Trim();
            var description = tender.Description?.Trim();
            var title = FirstNonEmpty(description, titleCode, $"Tender {externalKey}")!;
            var closing = TryParseDateOnly(tender.TenderPeriod?.EndDate);
            // Only keep listings that expire later than today.
            if (closing is null || closing.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            {
                continue;
            }

            var published = TryParseDateOnly(tender.TenderPeriod?.StartDate)
                ?? TryParseDateOnly(release.Date);
            var status = MapPortalStatus(tender.Status, closing);
            var docs = (tender.Documents ?? [])
                .Select(doc => doc.Url)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToArray();

            var summary = BuildSummary(tender, titleCode, published, closing);
            var canonical = BuildCanonicalUrl(portalBase, externalKey, docs);

            results.Add(new TenderCandidate(
                ExternalKey: externalKey,
                CanonicalUrl: canonical,
                Title: Truncate(title, 900)!,
                Summary: Truncate(summary, 3900),
                ClosingDate: closing,
                PortalStatus: status,
                DocumentUrls: docs));
        }

        return results;
    }

    private static bool LooksLikeOcdsPackage(string body)
    {
        var trimmed = body.AsSpan().TrimStart();
        if (trimmed.IsEmpty || trimmed[0] != '{')
        {
            return false;
        }

        return body.Contains("\"releases\"", StringComparison.OrdinalIgnoreCase)
            || body.Contains("\"ocid\"", StringComparison.OrdinalIgnoreCase);
    }

    private static Uri ResolvePortalBase(Uri sourceUri)
    {
        if (sourceUri.Host.Contains("etenders.gov.za", StringComparison.OrdinalIgnoreCase)
            && !sourceUri.Host.StartsWith("ocds-api.", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(sourceUri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/");
        }

        return new Uri("https://www.etenders.gov.za/");
    }

    private static string BuildCanonicalUrl(Uri portalBase, string externalKey, IReadOnlyList<string> docs)
    {
        if (docs.Count > 0)
        {
            try
            {
                return TenderUrlNormalizer.Canonicalize(docs[0], portalBase);
            }
            catch (InvalidOperationException)
            {
                // Fall through to ocid-based portal URL.
            }
        }

        return TenderUrlNormalizer.Canonicalize(
            portalBase.GetLeftPart(UriPartial.Path).TrimEnd('/') + "#" + Uri.EscapeDataString(externalKey),
            portalBase);
    }

    private static string BuildSummary(
        OcdsTender tender,
        string? titleCode,
        DateOnly? published,
        DateOnly? closing)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(titleCode))
        {
            parts.Add(titleCode);
        }

        if (!string.IsNullOrWhiteSpace(tender.Description))
        {
            parts.Add(tender.Description);
        }

        if (!string.IsNullOrWhiteSpace(tender.Category))
        {
            parts.Add($"Category: {tender.Category}");
        }

        if (!string.IsNullOrWhiteSpace(tender.Province))
        {
            parts.Add($"Province: {tender.Province}");
        }

        if (!string.IsNullOrWhiteSpace(tender.ProcuringEntity?.Name))
        {
            parts.Add($"Entity: {tender.ProcuringEntity.Name}");
        }

        if (!string.IsNullOrWhiteSpace(tender.ProcurementMethodDetails))
        {
            parts.Add(tender.ProcurementMethodDetails);
        }

        if (!string.IsNullOrWhiteSpace(tender.SpecialConditions))
        {
            parts.Add(tender.SpecialConditions);
        }

        if (!string.IsNullOrWhiteSpace(tender.DeliveryLocation))
        {
            parts.Add($"Location: {tender.DeliveryLocation}");
        }

        if (published is not null)
        {
            parts.Add($"Published: {published:yyyy-MM-dd}");
        }

        if (closing is not null)
        {
            parts.Add($"Closing: {closing:yyyy-MM-dd}");
        }

        return string.Join(" | ", parts);
    }

    private static TenderPortalStatus MapPortalStatus(string? status, DateOnly? closing)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "active" or "planned" => TenderPortalStatus.Open,
            "complete" or "completed" or "unsuccessful" => TenderPortalStatus.Closed,
            "cancelled" or "canceled" or "withdrawn" => TenderPortalStatus.Cancelled,
            "awarded" => TenderPortalStatus.Awarded,
            _ => HtmlTenderParser.InferPortalStatus(closing, status ?? string.Empty),
        };
    }

    private static DateOnly? TryParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return DateOnly.FromDateTime(dto.UtcDateTime);
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    private sealed class OcdsReleasePackage
    {
        public List<OcdsRelease>? Releases { get; set; }
    }

    private sealed class OcdsRelease
    {
        public string? Ocid { get; set; }
        public string? Id { get; set; }
        public string? Date { get; set; }
        public OcdsTender? Tender { get; set; }
    }

    private sealed class OcdsTender
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? Category { get; set; }
        public string? Province { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? SpecialConditions { get; set; }
        public string? Description { get; set; }
        public string? ProcurementMethodDetails { get; set; }
        public OcdsPeriod? TenderPeriod { get; set; }
        public OcdsEntity? ProcuringEntity { get; set; }
        public List<OcdsDocument>? Documents { get; set; }
    }

    private sealed class OcdsPeriod
    {
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }

    private sealed class OcdsEntity
    {
        public string? Name { get; set; }
    }

    private sealed class OcdsDocument
    {
        public string? Url { get; set; }
    }
}
