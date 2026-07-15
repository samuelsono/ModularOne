namespace CarTrack.Modules.Fleet;

public record VehicleLocationDto(
    double? Latitude,
    double? Longitude,
    string? PositionDescription,
    string? Updated,
    int? GpsFixType,
    IReadOnlyList<string>? GeofenceIds);

public record VehicleDriverDto(
    string? DriverId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? LicenseNumber);

public record VehicleFuelDto(
    double? Level,
    double? PercentageLeft,
    double? TotalConsumed,
    string? Updated);

public record VehicleElectricDto(
    double? BatteryPercentageLeft,
    string? ChargingStatus,
    string? BatteryTs,
    string? ChargingStatusTs);

public record VehicleTelemetryDto(
    string? EngineType,
    string? EventTs,
    double? Bearing,
    double? Speed,
    double? RoadSpeed,
    double? Odometer,
    double? Altitude,
    double? Rpm,
    bool? Ignition,
    bool? Idling,
    double? TcuBatteryPercentage,
    double? LvBatteryVoltage,
    double? UnitClock,
    double? WaterTemp,
    double? OilTemp,
    bool? CentralLockingStatus);

public record VehicleStatusDto(
    VehicleLocationDto? Location,
    VehicleDriverDto? Driver,
    VehicleFuelDto? Fuel,
    VehicleElectricDto? Electric,
    VehicleTelemetryDto? Telemetry,
    string? StatusSyncedAt);
