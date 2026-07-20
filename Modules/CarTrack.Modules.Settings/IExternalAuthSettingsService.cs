namespace CarTrack.Modules.Settings;

public interface IExternalAuthSettingsService
{
    Task<ExternalAuthSettingsDto> GetGoogleAsync(CancellationToken cancellationToken = default);

    Task<ExternalAuthSettingsDto> GetMicrosoftAsync(CancellationToken cancellationToken = default);

    Task<ExternalAuthSettingsDto> UpdateGoogleAsync(
        UpdateExternalAuthSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<ExternalAuthSettingsDto> UpdateMicrosoftAsync(
        UpdateExternalAuthSettingsRequest request,
        CancellationToken cancellationToken = default);
}
