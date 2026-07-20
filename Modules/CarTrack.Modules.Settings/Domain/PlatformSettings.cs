namespace CarTrack.Modules.Settings;

/// <summary>Singleton platform-wide settings (Id is always 1).</summary>
public class PlatformSettings
{
    public const int SingletonId = 1;

    public const string DefaultModuleSlug = "fleet";
    public const string DefaultThemeName = "talisLightTheme";

    // Default installed apps - all apps available on first install
    public const string DefaultInstalledAppSlugs = "accounting,leave,expense,payroll,performance,recruitment,tenders,fleet";

    public int Id { get; set; } = SingletonId;

    public string DefaultModuleSlugValue { get; set; } = DefaultModuleSlug;

    public string DefaultThemeNameValue { get; set; } = DefaultThemeName;

    /// <summary>Comma-separated list of installed app slugs that users can access.</summary>
    public string InstalledAppSlugs { get; set; } = DefaultInstalledAppSlugs;

    /// <summary>Serialized map in format moduleSlug:themeName,moduleSlug:themeName.</summary>
    public string AppThemeNamesByModuleSlug { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
