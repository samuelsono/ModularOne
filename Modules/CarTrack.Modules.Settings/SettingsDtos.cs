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

public record EmailSettingsDto(
    bool Enabled,
    string Provider,
    string FromAddress,
    string FromName,
    string? SmtpHost,
    int SmtpPort,
    bool UseSsl,
    string? Username,
    bool HasPassword,
    bool HasApiKey,
    string? MailgunDomain,
    string? UpdatedAt);

public record UpdateEmailSettingsRequest(
    bool Enabled,
    string Provider,
    string FromAddress,
    string FromName,
    string? SmtpHost = null,
    int SmtpPort = 587,
    bool UseSsl = true,
    string? Username = null,
    string? Password = null,
    string? ApiKey = null,
    string? MailgunDomain = null);

public record TestEmailSettingsRequest(string ToAddress);

public record TestEmailSettingsResponse(bool Success, string Message);
