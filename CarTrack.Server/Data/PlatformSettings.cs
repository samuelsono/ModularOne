namespace CarTrack.Server.Data;

/// <summary>
/// Singleton platform-wide settings (Id is always 1).
/// </summary>
public class PlatformSettings
{
    public const int SingletonId = 1;

    public const string DefaultModuleSlug = "fleet";

    public int Id { get; set; } = SingletonId;

    public string DefaultModuleSlugValue { get; set; } = DefaultModuleSlug;

    public DateTimeOffset UpdatedAt { get; set; }
}
