namespace CarTrack.Server.Settings;

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
    string? UpdatedAt);

public record UpdatePlatformSettingsRequest(
    string DefaultModuleSlug);

public record AppSettingsOverviewDto(
    string ApplicationName,
    string Version,
    CarTrackSettingsDto CarTrack,
    PlatformSettingsDto Platform);
