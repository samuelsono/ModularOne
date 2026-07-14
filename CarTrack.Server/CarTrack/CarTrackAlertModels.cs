using System.Text.Json.Serialization;

namespace CarTrack.Server.CarTrack;

public class CarTrackAlertsResponse
{
    [JsonPropertyName("data")]
    public List<CarTrackAlert> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public CarTrackMeta? Meta { get; set; }
}

public class CarTrackAlert
{
    [JsonPropertyName("alert_id")]
    public string? AlertId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("any_vehicle")]
    public bool AnyVehicle { get; set; }

    [JsonPropertyName("vehicles")]
    public List<CarTrackAlertVehicle> Vehicles { get; set; } = [];

    [JsonPropertyName("ignition_trigger_id")]
    public int? IgnitionTriggerId { get; set; }

    [JsonPropertyName("sensor_trigger_id")]
    public int? SensorTriggerId { get; set; }

    [JsonPropertyName("geofence_trigger_id")]
    public int? GeofenceTriggerId { get; set; }

    [JsonPropertyName("geofence_ids")]
    public List<string> GeofenceIds { get; set; } = [];

    [JsonPropertyName("geofence_group_ids")]
    public List<long> GeofenceGroupIds { get; set; } = [];

    [JsonPropertyName("inside_geofence")]
    public bool? InsideGeofence { get; set; }

    [JsonPropertyName("create_ts")]
    public string? CreateTs { get; set; }

    [JsonPropertyName("subuser_id")]
    public string? SubuserId { get; set; }

    [JsonPropertyName("contact_type")]
    public CarTrackAlertContactType? ContactType { get; set; }
}

public class CarTrackAlertVehicle
{
    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; set; }

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }

    [JsonPropertyName("chassis_number")]
    public string? ChassisNumber { get; set; }
}

public class CarTrackAlertContactType
{
    [JsonPropertyName("contact_type_id")]
    public int? ContactTypeId { get; set; }

    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = [];

    [JsonPropertyName("priority_id")]
    public long? PriorityId { get; set; }
}

public class CarTrackAlertNotificationsResponse
{
    [JsonPropertyName("data")]
    public List<CarTrackAlertNotification> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public CarTrackMeta? Meta { get; set; }
}

public class CarTrackAlertNotification
{
    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; set; }

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }

    [JsonPropertyName("trigger_description")]
    public string? TriggerDescription { get; set; }

    [JsonPropertyName("notification_contact")]
    public string? NotificationContact { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("contact_description")]
    public string? ContactDescription { get; set; }

    [JsonPropertyName("notification_msg")]
    public string? NotificationMsg { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("event_ts")]
    public string? EventTs { get; set; }

    [JsonPropertyName("geofence_id")]
    public string? GeofenceId { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }

    [JsonPropertyName("odometer")]
    public double? Odometer { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }
}
