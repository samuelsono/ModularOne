using System.Text;
using CarTrack.Infrastructure.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public sealed class ExternalAuthCredentialProvider(
    SettingsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<ExternalAuthCredentialProvider> logger) : IExternalAuthCredentialProvider
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("CarTrack.ExternalAuth.ClientSecret");

    public async Task<IReadOnlyList<ExternalAuthProviderInfo>> GetProviderStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var google = await GetCredentialsAsync(ExternalAuthProviders.Google, cancellationToken);
        var microsoft = await GetCredentialsAsync(ExternalAuthProviders.Microsoft, cancellationToken);

        return
        [
            new ExternalAuthProviderInfo(
                ExternalAuthProviders.Google,
                google is not null,
                google?.ClientId),
            new ExternalAuthProviderInfo(
                ExternalAuthProviders.Microsoft,
                microsoft is not null,
                microsoft?.ClientId),
        ];
    }

    public async Task<ExternalOAuthCredentials?> GetCredentialsAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeProvider(provider);
        if (normalized is null)
        {
            return null;
        }

        var settings = await dbContext.ExternalAuthSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Provider == normalized, cancellationToken);

        if (settings is null
            || string.IsNullOrWhiteSpace(settings.ClientId)
            || string.IsNullOrWhiteSpace(settings.ProtectedClientSecret))
        {
            return null;
        }

        var secret = UnprotectSecret(settings.ProtectedClientSecret);
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning(
                "External auth secret for {Provider} could not be decrypted. Re-save it under Settings → Integrations.",
                normalized);
            return null;
        }

        return new ExternalOAuthCredentials(
            normalized,
            settings.ClientId.Trim(),
            secret,
            settings.TenantId);
    }

    private static string? NormalizeProvider(string provider) =>
        provider.Trim().ToUpperInvariant() switch
        {
            "GOOGLE" => ExternalAuthProviders.Google,
            "MICROSOFT" => ExternalAuthProviders.Microsoft,
            _ => null,
        };

    private string? UnprotectSecret(string protectedSecret)
    {
        try
        {
            return Encoding.UTF8.GetString(_protector.Unprotect(Convert.FromBase64String(protectedSecret)));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unprotect external auth client secret.");
            return null;
        }
    }
}
