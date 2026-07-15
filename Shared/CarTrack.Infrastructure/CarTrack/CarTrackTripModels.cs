using System.Text.Json.Serialization;

namespace CarTrack.Server.CarTrack;

public class CarTrackTripsResponse
{
    [JsonPropertyName("data")]
    public List<CarTrackTrip> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public CarTrackMeta? Meta { get; set; }
}

public class CarTrackTrip
{
    [JsonPropertyName("trip_id")]
    public long TripId { get; set; }

    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; set; }

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }

    [JsonPropertyName("start_timestamp")]
    public string? StartTimestamp { get; set; }

    [JsonPropertyName("end_timestamp")]
    public string? EndTimestamp { get; set; }

    [JsonPropertyName("trip_duration")]
    public string? TripDuration { get; set; }

    [JsonPropertyName("trip_duration_seconds")]
    public long? TripDurationSeconds { get; set; }

    [JsonPropertyName("start_location")]
    public string? StartLocation { get; set; }

    [JsonPropertyName("start_coordinates")]
    public CarTrackCoordinates? StartCoordinates { get; set; }

    [JsonPropertyName("end_location")]
    public string? EndLocation { get; set; }

    [JsonPropertyName("end_coordinates")]
    public CarTrackCoordinates? EndCoordinates { get; set; }

    [JsonPropertyName("start_odometer")]
    public double? StartOdometer { get; set; }

    [JsonPropertyName("end_odometer")]
    public double? EndOdometer { get; set; }

    [JsonPropertyName("trip_distance")]
    public double? TripDistance { get; set; }

    [JsonPropertyName("start_geofence_name")]
    public string? StartGeofenceName { get; set; }

    [JsonPropertyName("end_geofence_name")]
    public string? EndGeofenceName { get; set; }

    [JsonPropertyName("thresholds_speeding_events")]
    public int? ThresholdsSpeedingEvents { get; set; }

    [JsonPropertyName("road_speeding_events")]
    public int? RoadSpeedingEvents { get; set; }

    [JsonPropertyName("max_speed")]
    public double? MaxSpeed { get; set; }

    [JsonPropertyName("harsh_braking_events")]
    public int? HarshBrakingEvents { get; set; }

    [JsonPropertyName("harsh_cornering_events")]
    public int? HarshCorneringEvents { get; set; }

    [JsonPropertyName("harsh_acceleration_events")]
    public int? HarshAccelerationEvents { get; set; }

    [JsonPropertyName("idle_time")]
    public string? IdleTime { get; set; }

    [JsonPropertyName("idle_time_seconds")]
    public long? IdleTimeSeconds { get; set; }

    [JsonPropertyName("driver_id")]
    public string? DriverId { get; set; }

    [JsonPropertyName("driver_name")]
    public string? DriverName { get; set; }

    [JsonPropertyName("driver_surname")]
    public string? DriverSurname { get; set; }

    [JsonPropertyName("trip_title")]
    public string? TripTitle { get; set; }

    [JsonPropertyName("trip_extra_notes")]
    public string? TripExtraNotes { get; set; }

    [JsonPropertyName("trip_type")]
    public string? TripType { get; set; }

    [JsonPropertyName("is_private")]
    public bool? IsPrivate { get; set; }
}

public class CarTrackCoordinates
{
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}
