using System.Text;
using CarTrack.Server.CarTrack;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Settings;

public class CarTrackCredentialProvider(
    SettingsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<CarTrackOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<CarTrackCredentialProvider> logger) : ICarTrackCredentialProvider
{
    private const string ProtectorPurpose = "CarTrack.Integration.Password";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

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

            logger.LogWarning(
                "CarTrackSettings password for user {Username} could not be decrypted. " +
                "Re-save the password under Settings → Integrations (DataProtection keys may have changed).",
                stored.Username);
        }

        var fallback = options.Value;
        return new CarTrackRuntimeCredentials(
            fallback.Username?.Trim() ?? string.Empty,
            fallback.Password ?? string.Empty,
            NormalizeBaseUrl(fallback.BaseUrl));
    }

    /// <summary>
    /// True when a stored blob exists and can be decrypted with the current (or legacy) key ring.
    /// </summary>
    public bool CanDecryptStoredPassword(string? protectedPassword)
    {
        if (string.IsNullOrWhiteSpace(protectedPassword))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(UnprotectPassword(protectedPassword));
    }

    private string? UnprotectPassword(string protectedPassword)
    {
        byte[] protectedBytes;
        try
        {
            protectedBytes = Convert.FromBase64String(protectedPassword);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "CarTrack API password blob is not valid Base64.");
            return null;
        }

        if (TryUnprotect(_protector, protectedBytes, out var password))
        {
            return password;
        }

        foreach (var legacyProvider in CreateLegacyProviders())
        {
            var protector = legacyProvider.CreateProtector(ProtectorPurpose);
            if (TryUnprotect(protector, protectedBytes, out password))
            {
                logger.LogInformation(
                    "Decrypted CarTrack API password using a legacy DataProtection key ring. Re-save credentials to re-encrypt with the current key ring.");
                return password;
            }
        }

        logger.LogWarning("Failed to unprotect CarTrack API password with current and legacy key rings.");
        return null;
    }

    private bool TryUnprotect(IDataProtector protector, byte[] protectedBytes, out string? password)
    {
        try
        {
            password = Encoding.UTF8.GetString(protector.Unprotect(protectedBytes));
            return !string.IsNullOrWhiteSpace(password);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "DataProtection unprotect attempt failed.");
            password = null;
            return false;
        }
    }

    private IEnumerable<IDataProtectionProvider> CreateLegacyProviders()
    {
        var defaultKeys = new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspnet",
            "DataProtection-Keys"));

        var appDataKeys = new DirectoryInfo(Path.Combine(
            hostEnvironment.ContentRootPath,
            "App_Data",
            "DataProtection-Keys"));

        var keyDirs = new[] { defaultKeys, appDataKeys }
            .Where(dir => dir.Exists)
            .DistinctBy(dir => dir.FullName, StringComparer.OrdinalIgnoreCase);

        var legacyAppNames = new List<string?>
        {
            "CarTrack",
            "CarTrack.Server",
            "CarTrack.Host",
            // Historical default: ContentRootPath of the old Server project.
            Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "..", "CarTrack.Server")),
            // Current Host content root (in case keys were created without SetApplicationName).
            hostEnvironment.ContentRootPath,
            null,
        };

        foreach (var keys in keyDirs)
        {
            foreach (var appName in legacyAppNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                IDataProtectionProvider provider;
                try
                {
                    provider = appName is null
                        ? DataProtectionProvider.Create(keys)
                        : DataProtectionProvider.Create(keys, builder => builder.SetApplicationName(appName));
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not create legacy DataProtection provider for {Keys}.", keys.FullName);
                    continue;
                }

                yield return provider;
            }
        }
    }

    internal static string ProtectPassword(IDataProtector protector, string password) =>
        Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(password)));

    internal static string NormalizeBaseUrl(string baseUrl) =>
        string.IsNullOrWhiteSpace(baseUrl)
            ? "https://fleetapi-za.cartrack.com"
            : baseUrl.Trim().TrimEnd('/');
}
