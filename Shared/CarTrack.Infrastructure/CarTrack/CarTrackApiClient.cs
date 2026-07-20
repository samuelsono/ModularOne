using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarTrack.Server.CarTrack;
using Microsoft.Extensions.Logging;

namespace CarTrack.Server.CarTrack;

public class CarTrackApiClient(
    HttpClient httpClient,
    ICarTrackCredentialProvider credentialProvider,
    ILogger<CarTrackApiClient> logger) : ICarTrackApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // CarTrack returns some numeric fields (e.g. vext) as JSON strings.
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public Task<CarTrackVehiclesResponse> GetVehiclesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<CarTrackVehiclesResponse>("rest/vehicles", cancellationToken);

    public Task<CarTrackVehicleStatusResponse> GetVehicleStatusesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<CarTrackVehicleStatusResponse>("rest/vehicles/status", cancellationToken);

    public Task<CarTrackVehicleEventsResponse> GetVehicleEventsAsync(
        string registration,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp,
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRangeQuery(startTimestamp, endTimestamp, page, perPage);
        var path = $"rest/vehicles/{Uri.EscapeDataString(registration)}/events{query}";
        return GetAsync<CarTrackVehicleEventsResponse>(path, cancellationToken);
    }

    public Task<CarTrackTripsResponse> GetTripsAsync(
        string registration,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp,
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRangeQuery(startTimestamp, endTimestamp, page, perPage);
        var path = $"rest/trips/{Uri.EscapeDataString(registration)}{query}";
        return GetAsync<CarTrackTripsResponse>(path, cancellationToken);
    }

    public Task<CarTrackAlertsResponse> GetAlertsAsync(
        int page,
        int perPage,
        CancellationToken cancellationToken = default) =>
        GetAsync<CarTrackAlertsResponse>($"rest/alerts?page={page}&per_page={perPage}", cancellationToken);

    public Task<CarTrackAlertNotificationsResponse> GetAlertNotificationsAsync(
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        const string format = "yyyy-MM-dd HH:mm:ss";
        var path = new StringBuilder("rest/alerts/notifications")
            .Append("?filter[date_from]=")
            .Append(Uri.EscapeDataString(dateFrom.ToString(format)))
            .Append("&filter[date_to]=")
            .Append(Uri.EscapeDataString(dateTo.ToString(format)))
            .Append("&page=")
            .Append(page)
            .Append("&per_page=")
            .Append(perPage)
            .ToString();

        return GetAsync<CarTrackAlertNotificationsResponse>(path, cancellationToken);
    }

    public async Task<JsonElement> GetDataArrayAsync(string path, CancellationToken cancellationToken = default)
    {
        var credentials = await credentialProvider.GetAsync(cancellationToken);

        EnsureCredentialsConfigured(credentials);

        var baseUri = new Uri($"{credentials.BaseUrl.TrimEnd('/')}/");
        var requestUri = new Uri(baseUri, path);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "CarTrack request to {Path} failed with status {StatusCode}: {Body}",
                path,
                (int)response.StatusCode,
                body.Length > 500 ? body[..500] : body);
            throw new CarTrackApiException(
                (int)response.StatusCode,
                $"CarTrack request failed with status {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"CarTrack response for {path} did not include a data array.");
        }

        return dataElement.Clone();
    }

    private static string BuildRangeQuery(
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp,
        int page,
        int perPage)
    {
        const string format = "yyyy-MM-dd HH:mm:ss";
        return new StringBuilder("?")
            .Append("start_timestamp=")
            .Append(Uri.EscapeDataString(startTimestamp.ToString(format)))
            .Append("&end_timestamp=")
            .Append(Uri.EscapeDataString(endTimestamp.ToString(format)))
            .Append("&page=")
            .Append(page)
            .Append("&per_page=")
            .Append(perPage)
            .ToString();
    }

    private static void EnsureCredentialsConfigured(CarTrackRuntimeCredentials credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            throw new InvalidOperationException(
                "CarTrack API credentials are not configured (or the stored password could not be decrypted). " +
                "Open Settings → Integrations → CarTrack API and save the password again.");
        }
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var credentials = await credentialProvider.GetAsync(cancellationToken);
        EnsureCredentialsConfigured(credentials);

        var baseUri = new Uri($"{credentials.BaseUrl.TrimEnd('/')}/");
        var requestUri = new Uri(baseUri, path);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "CarTrack request to {Path} failed with status {StatusCode}: {Body}",
                path,
                (int)response.StatusCode,
                body.Length > 500 ? body[..500] : body);
            throw new CarTrackApiException(
                (int)response.StatusCode,
                $"CarTrack request failed with status {(int)response.StatusCode}.");
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException($"CarTrack response for {path} was empty.");
    }
}
