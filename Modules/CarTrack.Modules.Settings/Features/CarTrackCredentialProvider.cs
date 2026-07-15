using System.Text;
using CarTrack.Server.CarTrack;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Settings;

public class CarTrackCredentialProvider(
    SettingsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<CarTrackOptions> options) : ICarTrackCredentialProvider
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("CarTrack.Integration.Password");

    public async Task<CarTrackRuntimeCredentials> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.CarTrackSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(settings => settings.Id == CarTrackSettings.SingletonId, cancellationToken);

        if (stored is not null
            && !string.IsNullOrWhiteSpace(stored.Username)
            && !string.IsNullOrWhiteSpace(stored.ProtectedPassword))
        {
            var password = UnprotectPassword(stored.ProtectedPassword);
            if (!string.IsNullOrWhiteSpace(password))
            {
                return new CarTrackRuntimeCredentials(
                    stored.Username.Trim(),
                    password,
                    NormalizeBaseUrl(stored.BaseUrl));
            }
        }

        var fallback = options.Value;
        return new CarTrackRuntimeCredentials(
            fallback.Username?.Trim() ?? string.Empty,
            fallback.Password ?? string.Empty,
            NormalizeBaseUrl(fallback.BaseUrl));
    }

    private string? UnprotectPassword(string protectedPassword)
    {
        try
        {
            return Encoding.UTF8.GetString(_protector.Unprotect(Convert.FromBase64String(protectedPassword)));
        }
        catch
        {
            return null;
        }
    }

    internal static string ProtectPassword(IDataProtector protector, string password) =>
        Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(password)));

    internal static string NormalizeBaseUrl(string baseUrl) =>
        string.IsNullOrWhiteSpace(baseUrl)
            ? "https://fleetapi-za.cartrack.com"
            : baseUrl.Trim().TrimEnd('/');
}
