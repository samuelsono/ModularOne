namespace CarTrack.Server.Data;

public class Vehicle : IAuditable
{
    public Guid Id { get; set; }

    public long? CarTrackVehicleId { get; set; }

    public required string RegistrationNumber { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Vin { get; set; } = string.Empty;

    public string EngineNumber { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;

    public string VehicleType { get; set; } = string.Empty;

    public string FuelType { get; set; } = "Diesel";

    public int Tare { get; set; }

    public int Gvm { get; set; }

    public string RegisteredOwner { get; set; } = string.Empty;

    public DateOnly? LicenceDiscExpiry { get; set; }

    public string IgnitionStatus { get; set; } = "off";

    public DateTimeOffset? CarTrackSyncedAt { get; set; }

    public DateTimeOffset? StatusSyncedAt { get; set; }

    public string? EngineType { get; set; }

    public DateTimeOffset? EventTs { get; set; }

    public double? Bearing { get; set; }

    public double? Speed { get; set; }

    public double? RoadSpeed { get; set; }

    public bool? Ignition { get; set; }

    public bool? Idling { get; set; }

    public double? Odometer { get; set; }

    public double? Altitude { get; set; }

    public double? Rpm { get; set; }

    public double? TcuBatteryPercentage { get; set; }

    public double? LvBatteryVoltage { get; set; }

    public double? UnitClock { get; set; }

    public double? WaterTemp { get; set; }

    public double? OilTemp { get; set; }

    public bool? CentralLockingStatus { get; set; }

    public string? GeofenceIdsJson { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTimeOffset? LocationUpdated { get; set; }

    public string? PositionDescription { get; set; }

    public int? GpsFixType { get; set; }

    public double? FuelLevel { get; set; }

    public double? FuelPercentageLeft { get; set; }

    public double? FuelTotalConsumed { get; set; }

    public DateTimeOffset? FuelUpdated { get; set; }

    public double? ElectricBatteryPercentage { get; set; }

    public string? ElectricChargingStatus { get; set; }

    public DateTimeOffset? ElectricBatteryTs { get; set; }

    public DateTimeOffset? ElectricChargingStatusTs { get; set; }

    public string? DriverId { get; set; }

    public string? DriverFirstName { get; set; }

    public string? DriverLastName { get; set; }

    public string? DriverPhone { get; set; }

    public string? DriverLicenseNumber { get; set; }

    public Guid? AssignedDriverId { get; set; }

    public Driver? AssignedDriver { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public bool IsLocalOnly => CarTrackVehicleId is null;

    public bool HasLocation => Latitude is not null && Longitude is not null;
}
