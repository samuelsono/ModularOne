using System.Text;
using CarTrack.Infrastructure.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public sealed class ExternalAuthSettingsService(
    SettingsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider) : IExternalAuthSettingsService
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("CarTrack.ExternalAuth.ClientSecret");

    public Task<ExternalAuthSettingsDto> GetGoogleAsync(CancellationToken cancellationToken = default) =>
        GetAsync(ExternalAuthProviders.Google, cancellationToken);

    public Task<ExternalAuthSettingsDto> GetMicrosoftAsync(CancellationToken cancellationToken = default) =>
        GetAsync(ExternalAuthProviders.Microsoft, cancellationToken);

    public Task<ExternalAuthSettingsDto> UpdateGoogleAsync(
        UpdateExternalAuthSettingsRequest request,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(ExternalAuthProviders.Google, request, cancellationToken);

    public Task<ExternalAuthSettingsDto> UpdateMicrosoftAsync(
        UpdateExternalAuthSettingsRequest request,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(ExternalAuthProviders.Microsoft, request, cancellationToken);

    private async Task<ExternalAuthSettingsDto> GetAsync(string provider, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(provider, cancellationToken);
        return ToDto(settings);
    }

    private async Task<ExternalAuthSettingsDto> UpdateAsync(
        string provider,
        UpdateExternalAuthSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new ArgumentException("Client ID is required.", nameof(request));
        }

        var settings = await GetOrCreateAsync(provider, cancellationToken);
        settings.ClientId = request.ClientId.Trim();

        if (provider == ExternalAuthProviders.Microsoft)
        {
            settings.TenantId = string.IsNullOrWhiteSpace(request.TenantId)
                ? "common"
                : request.TenantId.Trim();
        }
        else
        {
            settings.TenantId = null;
        }

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            settings.ProtectedClientSecret = ProtectSecret(request.ClientSecret.Trim());
        }
        else if (string.IsNullOrWhiteSpace(settings.ProtectedClientSecret)
                 || UnprotectSecret(settings.ProtectedClientSecret) is null)
        {
            throw new InvalidOperationException(
                "Client secret is required when saving OAuth credentials for the first time (or when the existing secret cannot be decrypted).");
        }

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private async Task<ExternalAuthSettings> GetOrCreateAsync(string provider, CancellationToken cancellationToken)
    {
        var settings = await dbContext.ExternalAuthSettings
            .FirstOrDefaultAsync(item => item.Provider == provider, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new ExternalAuthSettings
        {
            Provider = provider,
            TenantId = provider == ExternalAuthProviders.Microsoft ? "common" : null,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.ExternalAuthSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private ExternalAuthSettingsDto ToDto(ExternalAuthSettings settings)
    {
        var secret = UnprotectSecret(settings.ProtectedClientSecret);
        var hasSecret = !string.IsNullOrWhiteSpace(secret);
        var isActivated = !string.IsNullOrWhiteSpace(settings.ClientId) && hasSecret;

        return new ExternalAuthSettingsDto(
            settings.Provider,
            settings.ClientId,
            settings.TenantId,
            hasSecret,
            isActivated,
            settings.UpdatedAt == default ? null : settings.UpdatedAt.ToString("O"));
    }

    private string ProtectSecret(string secret) =>
        Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(secret)));

    private string? UnprotectSecret(string? protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(_protector.Unprotect(Convert.FromBase64String(protectedSecret)));
        }
        catch
        {
            return null;
        }
    }
}
