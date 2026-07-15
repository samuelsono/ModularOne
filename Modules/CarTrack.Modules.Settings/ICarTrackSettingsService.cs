namespace CarTrack.Modules.Settings;

public interface ICarTrackSettingsService
{
    Task<CarTrackSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<CarTrackSettingsDto> UpdateAsync(
        UpdateCarTrackSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<TestCarTrackConnectionResponse> TestConnectionAsync(CancellationToken cancellationToken = default);
}
