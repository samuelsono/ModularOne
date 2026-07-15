using System.Net;
using System.Text.Json;

namespace CarTrack.Modules.Leave;

public interface IOpenHolidaysApiClient
{
    Task<IReadOnlyList<OpenHolidayEntry>> GetPublicHolidaysAsync(
        DateOnly validFrom,
        DateOnly validTo,
        CancellationToken cancellationToken = default);
}

public record OpenHolidayEntry(
    string ExternalId,
    string Name,
    DateOnly Date);

internal sealed class OpenHolidaysApiClient(HttpClient httpClient) : IOpenHolidaysApiClient
{
    private const string CountryIsoCode = "ZA";
    private const string LanguageIsoCode = "ZA";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<OpenHolidayEntry>> GetPublicHolidaysAsync(
        DateOnly validFrom,
        DateOnly validTo,
        CancellationToken cancellationToken = default)
    {
        var path =
            $"/PublicHolidays?countryIsoCode={CountryIsoCode}" +
            $"&validFrom={validFrom:yyyy-MM-dd}" +
            $"&validTo={validTo:yyyy-MM-dd}" +
            $"&languageIsoCode={LanguageIsoCode}";

        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<OpenHolidayApiResponse[]>(stream, JsonOptions, cancellationToken)
            ?? [];

        return payload
            .Where(item => string.Equals(item.Type, "Public", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item =>
            {
                if (!DateOnly.TryParse(item.StartDate, out var startDate))
                {
                    return [];
                }

                var name = item.Name
                    .FirstOrDefault(entry => string.Equals(entry.Language, "EN", StringComparison.OrdinalIgnoreCase))
                    ?.Text
                    ?? item.Name.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return [];
                }

                return new[] { new OpenHolidayEntry(item.Id, name.Trim(), startDate) };
            })
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private sealed class OpenHolidayApiResponse
    {
        public string Id { get; set; } = string.Empty;

        public string StartDate { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public OpenHolidayName[] Name { get; set; } = [];
    }

    private sealed class OpenHolidayName
    {
        public string Language { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }
}
