using System.Text.Json.Serialization;

namespace CarTrack.Server.CarTrack;

public class CarTrackVehiclesResponse
{
    [JsonPropertyName("data")]
    public List<CarTrackVehicle> Data { get; set; } = [];

    [JsonPropertyName("meta")]
    public CarTrackMeta? Meta { get; set; }
}

public class CarTrackVehicle
{
    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; set; }

    [JsonPropertyName("terminal_id")]
    public long? TerminalId { get; set; }

    [JsonPropertyName("terminal_serial")]
    public string? TerminalSerial { get; set; }

    [JsonPropertyName("registration")]
    public string? Registration { get; set; }

    [JsonPropertyName("vehicle_name")]
    public string? VehicleName { get; set; }

    [JsonPropertyName("client_vehicle_description")]
    public string? ClientVehicleDescription { get; set; }

    [JsonPropertyName("licence_expiry_date")]
    public string? LicenceExpiryDate { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("model_year")]
    public int? ModelYear { get; set; }

    [JsonPropertyName("colour")]
    public string? Colour { get; set; }

    [JsonPropertyName("chassis_number")]
    public string? ChassisNumber { get; set; }

    [JsonPropertyName("is_under_maintenance")]
    public bool IsUnderMaintenance { get; set; }

    [JsonPropertyName("vehicle_type")]
    public string? VehicleType { get; set; }

    [JsonPropertyName("sensors")]
    public CarTrackSensors? Sensors { get; set; }
}

public class CarTrackSensors
{
    [JsonPropertyName("electric_battery")]
    public bool ElectricBattery { get; set; }

    [JsonPropertyName("electric_charging")]
    public bool ElectricCharging { get; set; }
}

public class CarTrackMeta
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
