namespace CarTrack.Modules.Settings;

/// <summary>Singleton integration settings for the CarTrack Fleet API (Id is always 1).</summary>
public class CarTrackSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public string BaseUrl { get; set; } = "https://fleetapi-za.cartrack.com";

    public string Username { get; set; } = string.Empty;

    public string? ProtectedPassword { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
