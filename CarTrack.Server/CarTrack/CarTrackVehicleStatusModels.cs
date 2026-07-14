using System.Text.Json.Serialization;

namespace CarTrack.Server.CarTrack;

public class CarTrackVehicleStatusResponse
{
    [JsonPropertyName("data")]
    public List<CarTrackVehicleStatus> Data { get; set; } = [];
}

public class CarTrackVehicleStatus
{
    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; set; }

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }

    [JsonPropertyName("engine_type")]
    public string? EngineType { get; set; }

    [JsonPropertyName("chassis_number")]
    public string? ChassisNumber { get; set; }

    [JsonPropertyName("event_ts")]
    public string? EventTs { get; set; }

    [JsonPropertyName("bearing")]
    public double? Bearing { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    [JsonPropertyName("ignition")]
    public bool? Ignition { get; set; }

    [JsonPropertyName("idling")]
    public bool? Idling { get; set; }

    [JsonPropertyName("odometer")]
    public double? Odometer { get; set; }

    [JsonPropertyName("clock")]
    public double? Clock { get; set; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; set; }

    [JsonPropertyName("rpm")]
    public double? Rpm { get; set; }

    [JsonPropertyName("road_speed")]
    public double? RoadSpeed { get; set; }

    [JsonPropertyName("vext")]
    public double? Vext { get; set; }

    [JsonPropertyName("temp1")]
    public double? Temp1 { get; set; }

    [JsonPropertyName("temp2")]
    public double? Temp2 { get; set; }

    [JsonPropertyName("central_locking_status")]
    public bool? CentralLockingStatus { get; set; }

    [JsonPropertyName("tcu_battery_percentage")]
    public double? TcuBatteryPercentage { get; set; }

    [JsonPropertyName("driver")]
    public CarTrackDriver? Driver { get; set; }

    [JsonPropertyName("fuel")]
    public CarTrackFuel? Fuel { get; set; }

    [JsonPropertyName("electric")]
    public CarTrackElectric? Electric { get; set; }

    [JsonPropertyName("location")]
    public CarTrackLocation? Location { get; set; }
}

public class CarTrackDriver
{
    [JsonPropertyName("driver_id")]
    public string? DriverId { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("id_number")]
    public string? IdNumber { get; set; }

    [JsonPropertyName("license_number")]
    public string? LicenseNumber { get; set; }

    [JsonPropertyName("tag_id")]
    public string? TagId { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }
}

public class CarTrackFuel
{
    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("level")]
    public double? Level { get; set; }

    [JsonPropertyName("percentage_left")]
    public double? PercentageLeft { get; set; }

    [JsonPropertyName("total_consumed")]
    public double? TotalConsumed { get; set; }
}

public class CarTrackElectric
{
    [JsonPropertyName("battery_percentage_left")]
    public double? BatteryPercentageLeft { get; set; }

    [JsonPropertyName("battery_ts")]
    public string? BatteryTs { get; set; }

    [JsonPropertyName("charging_status")]
    public string? ChargingStatus { get; set; }

    [JsonPropertyName("charging_status_ts")]
    public string? ChargingStatusTs { get; set; }
}

public class CarTrackLocation
{
    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("gps_fix_type")]
    public int? GpsFixType { get; set; }

    [JsonPropertyName("position_description")]
    public string? PositionDescription { get; set; }

    [JsonPropertyName("geofence_ids")]
    public List<string>? GeofenceIds { get; set; }
}
