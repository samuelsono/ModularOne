namespace CarTrack.Modules.Settings;

public record CarTrackSettingsDto(
    string BaseUrl,
    string Username,
    bool HasPassword,
    string? UpdatedAt);

public record UpdateCarTrackSettingsRequest(
    string BaseUrl,
    string Username,
    string? Password);

public record TestCarTrackConnectionResponse(
    bool Success,
    string Message);

public record PlatformSettingsDto(
    string DefaultModuleSlug,
    string[] InstalledAppSlugs,
    string DefaultThemeName,
    Dictionary<string, string> AppThemeNamesByModuleSlug,
    string? UpdatedAt);

public record UpdatePlatformSettingsRequest(
    string? DefaultModuleSlug = null,
    string[]? InstalledAppSlugs = null,
    string? DefaultThemeName = null,
    Dictionary<string, string>? AppThemeNamesByModuleSlug = null);

public record AppSettingsOverviewDto(
    string ApplicationName,
    string Version,
    CarTrackSettingsDto CarTrack,
    PlatformSettingsDto Platform);

public record ExternalAuthSettingsDto(
    string Provider,
    string ClientId,
    string? TenantId,
    bool HasClientSecret,
    bool IsActivated,
    string? UpdatedAt);

public record UpdateExternalAuthSettingsRequest(
    string ClientId,
    string? ClientSecret,
    string? TenantId);
