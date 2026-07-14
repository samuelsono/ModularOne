namespace CarTrack.Server.Configuration;

public class AppUrlOptions
{
    public const string SectionName = "AppUrl";

    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
