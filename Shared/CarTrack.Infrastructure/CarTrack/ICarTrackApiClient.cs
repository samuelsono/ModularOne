using System.Text.Json;

namespace CarTrack.Server.CarTrack;

public interface ICarTrackApiClient
{
    Task<CarTrackVehiclesResponse> GetVehiclesAsync(CancellationToken cancellationToken = default);

    Task<CarTrackVehicleStatusResponse> GetVehicleStatusesAsync(CancellationToken cancellationToken = default);

    Task<CarTrackVehicleEventsResponse> GetVehicleEventsAsync(
        string registration,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp,
        int page,
        int perPage,
        CancellationToken cancellationToken = default);

    Task<CarTrackTripsResponse> GetTripsAsync(
        string registration,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp,
        int page,
        int perPage,
        CancellationToken cancellationToken = default);

    Task<CarTrackAlertsResponse> GetAlertsAsync(
        int page,
        int perPage,
        CancellationToken cancellationToken = default);

    Task<CarTrackAlertNotificationsResponse> GetAlertNotificationsAsync(
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        int page,
        int perPage,
        CancellationToken cancellationToken = default);

    Task<JsonElement> GetDataArrayAsync(string path, CancellationToken cancellationToken = default);
}
