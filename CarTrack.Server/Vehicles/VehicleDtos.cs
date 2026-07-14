namespace CarTrack.Server.Vehicles;

public record VehicleDto(
    string Id,
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    string Vin,
    string EngineNumber,
    string Colour,
    string VehicleType,
    string FuelType,
    int Tare,
    int Gvm,
    string RegisteredOwner,
    string? LicenceDiscExpiry,
    string IgnitionStatus,
    bool IsLocalOnly,
    string? CarTrackSyncedAt,
    VehicleStatusDto? Status,
    string? AssignedDriverId,
    string? AssignedDriverName,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName);

public record VehiclesResponse(
    IReadOnlyList<VehicleDto> Items,
    int Total,
    string? LastSyncedAt,
    bool IsCacheFresh,
    int CacheTtlMinutes);

public record CreateVehicleRequest(
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    string Colour,
    string VehicleType,
    string FuelType,
    string? Vin,
    string? EngineNumber,
    int Tare,
    int Gvm,
    string? RegisteredOwner,
    string? LicenceDiscExpiry,
    Guid? AssignedDriverId = null);

public record UpdateVehicleRequest(
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    string Colour,
    string VehicleType,
    string FuelType,
    string? Vin,
    string? EngineNumber,
    int Tare,
    int Gvm,
    string? RegisteredOwner,
    string? LicenceDiscExpiry,
    Guid? AssignedDriverId = null);

public record VehicleSyncResponse(
    int Created,
    int Updated,
    IReadOnlyList<VehicleDto> Items,
    int Total,
    string? LastSyncedAt,
    bool IsCacheFresh,
    int CacheTtlMinutes);

public record DeleteVehiclesRequest(IReadOnlyList<Guid> Ids);

public record DeleteVehiclesResponse(int DeletedCount);
