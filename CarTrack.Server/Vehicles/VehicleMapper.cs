using System.Text.Json;
using CarTrack.Server.Data;

namespace CarTrack.Server.Vehicles;

public static class VehicleMapper
{
    public static VehicleDto ToDto(Vehicle vehicle) => new(
        Id: vehicle.Id.ToString(),
        RegistrationNumber: vehicle.RegistrationNumber,
        Make: vehicle.Make,
        Model: vehicle.Model,
        Year: vehicle.Year,
        Vin: vehicle.Vin,
        EngineNumber: vehicle.EngineNumber,
        Colour: vehicle.Colour,
        VehicleType: vehicle.VehicleType,
        FuelType: vehicle.FuelType,
        Tare: vehicle.Tare,
        Gvm: vehicle.Gvm,
        RegisteredOwner: vehicle.RegisteredOwner,
        LicenceDiscExpiry: vehicle.LicenceDiscExpiry?.ToString("yyyy-MM-dd"),
        IgnitionStatus: vehicle.IgnitionStatus,
        IsLocalOnly: vehicle.IsLocalOnly,
        CarTrackSyncedAt: vehicle.CarTrackSyncedAt?.ToString("O"),
        Status: ToStatusDto(vehicle),
        AssignedDriverId: vehicle.AssignedDriverId?.ToString(),
        AssignedDriverName: vehicle.AssignedDriver is null
            ? null
            : $"{vehicle.AssignedDriver.FirstName} {vehicle.AssignedDriver.LastName}".Trim(),
        CreatedAt: AuditableMapping.FormatTimestamp(vehicle.CreatedAt),
        CreatedByUserId: vehicle.CreatedByUserId,
        CreatedByDisplayName: AuditableMapping.UserDisplayName(vehicle.CreatedByUser),
        UpdatedAt: AuditableMapping.FormatTimestamp(vehicle.UpdatedAt == default ? vehicle.CreatedAt : vehicle.UpdatedAt),
        UpdatedByUserId: vehicle.UpdatedByUserId,
        UpdatedByDisplayName: AuditableMapping.UserDisplayName(vehicle.UpdatedByUser));

    private static VehicleStatusDto? ToStatusDto(Vehicle vehicle)
    {
        if (vehicle.StatusSyncedAt is null
            && vehicle.Latitude is null
            && vehicle.Longitude is null
            && vehicle.EventTs is null)
        {
            return null;
        }

        return new VehicleStatusDto(
            Location: vehicle.Latitude is not null && vehicle.Longitude is not null
                ? new VehicleLocationDto(
                    vehicle.Latitude,
                    vehicle.Longitude,
                    vehicle.PositionDescription,
                    vehicle.LocationUpdated?.ToString("O"),
                    vehicle.GpsFixType,
                    ParseGeofenceIds(vehicle.GeofenceIdsJson))
                : vehicle.GeofenceIdsJson is not null
                    ? new VehicleLocationDto(
                        null,
                        null,
                        vehicle.PositionDescription,
                        vehicle.LocationUpdated?.ToString("O"),
                        vehicle.GpsFixType,
                        ParseGeofenceIds(vehicle.GeofenceIdsJson))
                    : null,
            Driver: HasDriver(vehicle)
                ? new VehicleDriverDto(
                    vehicle.DriverId,
                    vehicle.DriverFirstName,
                    vehicle.DriverLastName,
                    vehicle.DriverPhone,
                    vehicle.DriverLicenseNumber)
                : null,
            Fuel: vehicle.FuelLevel is not null || vehicle.FuelPercentageLeft is not null
                ? new VehicleFuelDto(
                    vehicle.FuelLevel,
                    vehicle.FuelPercentageLeft,
                    vehicle.FuelTotalConsumed,
                    vehicle.FuelUpdated?.ToString("O"))
                : null,
            Electric: vehicle.ElectricBatteryPercentage is not null || vehicle.ElectricChargingStatus is not null
                ? new VehicleElectricDto(
                    vehicle.ElectricBatteryPercentage,
                    vehicle.ElectricChargingStatus,
                    vehicle.ElectricBatteryTs?.ToString("O"),
                    vehicle.ElectricChargingStatusTs?.ToString("O"))
                : null,
            Telemetry: new VehicleTelemetryDto(
                vehicle.EngineType,
                vehicle.EventTs?.ToString("O"),
                vehicle.Bearing,
                vehicle.Speed,
                vehicle.RoadSpeed,
                vehicle.Odometer,
                vehicle.Altitude,
                vehicle.Rpm,
                vehicle.Ignition,
                vehicle.Idling,
                vehicle.TcuBatteryPercentage,
                vehicle.LvBatteryVoltage,
                vehicle.UnitClock,
                vehicle.WaterTemp,
                vehicle.OilTemp,
                vehicle.CentralLockingStatus),
            StatusSyncedAt: vehicle.StatusSyncedAt?.ToString("O"));
    }

    private static bool HasDriver(Vehicle vehicle) =>
        !string.IsNullOrWhiteSpace(vehicle.DriverId)
        || !string.IsNullOrWhiteSpace(vehicle.DriverFirstName)
        || !string.IsNullOrWhiteSpace(vehicle.DriverLastName);

    private static IReadOnlyList<string>? ParseGeofenceIds(string? geofenceIdsJson)
    {
        if (string.IsNullOrWhiteSpace(geofenceIdsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(geofenceIdsJson);
        }
        catch
        {
            return null;
        }
    }
}
