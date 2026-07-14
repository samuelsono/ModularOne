using System.Text.Json.Serialization;

namespace CarTrack.Server.CarTrack;

public class CarTrackVehicleEventsResponse
{
    [JsonPropertyName("data")]
    public List<CarTrackVehicleEvent> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public CarTrackMeta? Meta { get; set; }
}

public class CarTrackVehicleEvent
{
    [JsonPropertyName("event_id")]
    public long EventId { get; set; }

    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; set; }

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }

    [JsonPropertyName("chassis_number")]
    public string? ChassisNumber { get; set; }

    [JsonPropertyName("terminal_event_type_id")]
    public int? TerminalEventTypeId { get; set; }

    [JsonPropertyName("event_description")]
    public string? EventDescription { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; set; }

    [JsonPropertyName("odometer")]
    public double? Odometer { get; set; }

    [JsonPropertyName("clock")]
    public double? Clock { get; set; }

    [JsonPropertyName("bearing")]
    public double? Bearing { get; set; }

    [JsonPropertyName("ignition")]
    public bool? Ignition { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    [JsonPropertyName("road_speed")]
    public double? RoadSpeed { get; set; }

    [JsonPropertyName("rpm")]
    public double? Rpm { get; set; }

    [JsonPropertyName("road_speeding")]
    public bool? RoadSpeeding { get; set; }

    [JsonPropertyName("position_description")]
    public string? PositionDescription { get; set; }

    [JsonPropertyName("vext")]
    public double? Vext { get; set; }

    [JsonPropertyName("battery_percentage_left")]
    public double? BatteryPercentageLeft { get; set; }

    [JsonPropertyName("event_ts")]
    public string? EventTs { get; set; }

    [JsonPropertyName("received_ts")]
    public string? ReceivedTs { get; set; }

    [JsonPropertyName("gps_fix_type")]
    public int? GpsFixType { get; set; }

    [JsonPropertyName("water_temp")]
    public double? WaterTemp { get; set; }

    [JsonPropertyName("oil_temp")]
    public double? OilTemp { get; set; }

    [JsonPropertyName("driver_id")]
    public string? DriverId { get; set; }
}
