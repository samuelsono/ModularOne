using CarTrack.Server.CarTrack;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public class CarTrackSettingsService(
    SettingsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    ICarTrackCredentialProvider credentialProvider,
    CarTrackCredentialProvider settingsCredentialProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<CarTrackSettingsService> logger) : ICarTrackSettingsService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("CarTrack.Integration.Password");

    public async Task<CarTrackSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<CarTrackSettingsDto> UpdateAsync(
        UpdateCarTrackSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            throw new ArgumentException("Base URL is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Username is required.", nameof(request));
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.BaseUrl = CarTrackCredentialProvider.NormalizeBaseUrl(request.BaseUrl);
        settings.Username = request.Username.Trim();

        var canUseExistingPassword = settingsCredentialProvider.CanDecryptStoredPassword(settings.ProtectedPassword);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            settings.ProtectedPassword = CarTrackCredentialProvider.ProtectPassword(_protector, request.Password);
        }
        else if (!canUseExistingPassword)
        {
            throw new InvalidOperationException(
                "Password is required. The saved password can no longer be decrypted (encryption keys changed). Enter the CarTrack API password and save again.");
        }

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    public async Task<TestCarTrackConnectionResponse> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var credentials = await credentialProvider.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            return new TestCarTrackConnectionResponse(
                false,
                "CarTrack username and password are not available. Enter the API password under Settings → Integrations and save again.");
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(CarTrackSettingsService));
            var baseUri = new Uri($"{credentials.BaseUrl.TrimEnd('/')}/");
            var requestUri = new Uri(baseUri, "rest/vehicles");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var encoded = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{credentials.Username}:{credentials.Password}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new TestCarTrackConnectionResponse(true, "Successfully connected to the CarTrack Fleet API.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "CarTrack connection test failed with status {StatusCode}: {Body}",
                (int)response.StatusCode,
                body.Length > 300 ? body[..300] : body);

            return new TestCarTrackConnectionResponse(
                false,
                $"CarTrack API returned status {(int)response.StatusCode}. Check your credentials and base URL.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CarTrack connection test failed.");
            return new TestCarTrackConnectionResponse(false, "Could not reach the CarTrack API. Verify the base URL and network access.");
        }
    }

    private async Task<CarTrackSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.CarTrackSettings
            .FirstOrDefaultAsync(item => item.Id == CarTrackSettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new CarTrackSettings
        {
            Id = CarTrackSettings.SingletonId,
            BaseUrl = "https://fleetapi-za.cartrack.com",
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.CarTrackSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private CarTrackSettingsDto ToDto(CarTrackSettings settings)
    {
        // Only report hasPassword when the blob is actually usable.
        var hasUsablePassword = settingsCredentialProvider.CanDecryptStoredPassword(settings.ProtectedPassword);
        return new(
            settings.BaseUrl,
            settings.Username,
            hasUsablePassword,
            settings.UpdatedAt == default ? null : settings.UpdatedAt.ToString("O"));
    }
}
