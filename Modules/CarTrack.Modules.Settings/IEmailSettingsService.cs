namespace CarTrack.Modules.Settings;

public interface IEmailSettingsService
{
    Task<EmailSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<EmailSettingsDto> UpdateAsync(
        UpdateEmailSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<TestEmailSettingsResponse> TestAsync(
        TestEmailSettingsRequest request,
        CancellationToken cancellationToken = default);
}
