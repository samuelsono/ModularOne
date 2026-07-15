namespace CarTrack.Modules.Fleet;

public record VehicleEventDto(
    long EventId,
    string? EventDescription,
    int? TerminalEventTypeId,
    string? EventTs,
    string? ReceivedTs,
    double? Latitude,
    double? Longitude,
    double? Altitude,
    double? Odometer,
    double? Bearing,
    double? Speed,
    double? RoadSpeed,
    double? Rpm,
    bool? Ignition,
    bool? RoadSpeeding,
    string? PositionDescription,
    double? Vext,
    double? BatteryPercentageLeft,
    double? WaterTemp,
    double? OilTemp,
    int? GpsFixType,
    string? DriverId);

public record PaginationDto(
    int Page,
    int PerPage,
    int LastPage,
    int Total);

public record VehicleEventsResponse(
    string Registration,
    string Start,
    string End,
    IReadOnlyList<VehicleEventDto> Items,
    PaginationDto Pagination,
    bool FromCache);
