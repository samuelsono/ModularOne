namespace CarTrack.Modules.Settings;

public interface IPlatformSettingsService
{
    Task<PlatformSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<PlatformSettingsDto> UpdateAsync(
        UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken = default);
}
