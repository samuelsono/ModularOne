namespace CarTrack.Server.CarTrack;

public class CarTrackOptions
{
    public const string SectionName = "CarTrack";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://fleetapi-za.cartrack.com";
}
