namespace CarTrack.Server.Vehicles;

public record TripCoordinatesDto(double? Latitude, double? Longitude);

public record TripDto(
    long TripId,
    string? StartTimestamp,
    string? EndTimestamp,
    string? TripDuration,
    long? TripDurationSeconds,
    string? StartLocation,
    TripCoordinatesDto? StartCoordinates,
    string? EndLocation,
    TripCoordinatesDto? EndCoordinates,
    double? StartOdometer,
    double? EndOdometer,
    double? TripDistance,
    string? StartGeofenceName,
    string? EndGeofenceName,
    int? ThresholdsSpeedingEvents,
    int? RoadSpeedingEvents,
    double? MaxSpeed,
    int? HarshBrakingEvents,
    int? HarshCorneringEvents,
    int? HarshAccelerationEvents,
    string? IdleTime,
    long? IdleTimeSeconds,
    string? DriverId,
    string? DriverName,
    string? TripTitle,
    string? TripType,
    bool? IsPrivate);

public record TripsResponse(
    string Registration,
    string Start,
    string End,
    IReadOnlyList<TripDto> Items,
    PaginationDto Pagination,
    bool FromCache);
