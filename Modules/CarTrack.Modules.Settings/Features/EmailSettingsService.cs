using System.Text;
using CarTrack.Infrastructure.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Settings;

public sealed class EmailSettingsService(
    SettingsDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IEmailDispatcher emailDispatcher,
    ILogger<EmailSettingsService> logger) : IEmailSettingsService
{
    private const string ProtectorPurpose = "CarTrack.Email.Secrets";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public async Task<EmailSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<EmailSettingsDto> UpdateAsync(
        UpdateEmailSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FromAddress))
        {
            throw new ArgumentException("From address is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FromName))
        {
            throw new ArgumentException("From name is required.", nameof(request));
        }

        var provider = EmailProviders.Normalize(request.Provider);
        var settings = await GetOrCreateAsync(cancellationToken);

        settings.Enabled = request.Enabled;
        settings.Provider = provider;
        settings.FromAddress = request.FromAddress.Trim();
        settings.FromName = request.FromName.Trim();
        settings.SmtpHost = string.IsNullOrWhiteSpace(request.SmtpHost) ? null : request.SmtpHost.Trim();
        settings.SmtpPort = request.SmtpPort > 0 ? request.SmtpPort : 587;
        settings.UseSsl = request.UseSsl;
        settings.Username = string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim();
        settings.MailgunDomain = string.IsNullOrWhiteSpace(request.MailgunDomain)
            ? null
            : request.MailgunDomain.Trim();

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            settings.ProtectedPassword = ProtectSecret(request.Password.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            settings.ProtectedApiKey = ProtectSecret(request.ApiKey.Trim());
        }

        ValidateProviderSecrets(settings, provider);

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<TestEmailSettingsResponse> TestAsync(
        TestEmailSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ToAddress))
        {
            return new TestEmailSettingsResponse(false, "Enter a recipient email address.");
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        var config = TryBuildRuntimeConfig(settings);
        if (config is null)
        {
            return new TestEmailSettingsResponse(
                false,
                "Email is not fully configured. Enable a provider, fill required fields, and save before testing.");
        }

        try
        {
            await emailDispatcher.SendAsync(
                config,
                request.ToAddress.Trim(),
                "TalisTrack email test",
                "<p>This is a test email from TalisTrack email settings.</p>",
                cancellationToken);

            return new TestEmailSettingsResponse(true, $"Test email sent to {request.ToAddress.Trim()}.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Email settings test failed.");
            return new TestEmailSettingsResponse(false, ex.Message);
        }
    }

    internal EmailRuntimeConfig? TryBuildRuntimeConfig(EmailSettings settings)
    {
        if (!settings.Enabled)
        {
            return null;
        }

        var provider = EmailProviders.Normalize(settings.Provider);
        var password = UnprotectSecret(settings.ProtectedPassword);
        var apiKey = UnprotectSecret(settings.ProtectedApiKey);

        return provider switch
        {
            EmailProviders.SendGrid when !string.IsNullOrWhiteSpace(apiKey) => new EmailRuntimeConfig(
                EmailProviderNames.SendGrid,
                settings.FromAddress,
                settings.FromName,
                ApiKey: apiKey),
            EmailProviders.Mailgun when !string.IsNullOrWhiteSpace(apiKey)
                && !string.IsNullOrWhiteSpace(settings.MailgunDomain) => new EmailRuntimeConfig(
                EmailProviderNames.Mailgun,
                settings.FromAddress,
                settings.FromName,
                ApiKey: apiKey,
                MailgunDomain: settings.MailgunDomain),
            EmailProviders.Smtp when !string.IsNullOrWhiteSpace(settings.SmtpHost) => new EmailRuntimeConfig(
                EmailProviderNames.Smtp,
                settings.FromAddress,
                settings.FromName,
                SmtpHost: settings.SmtpHost,
                SmtpPort: settings.SmtpPort > 0 ? settings.SmtpPort : 587,
                UseSsl: settings.UseSsl,
                Username: settings.Username,
                Password: password),
            _ => null,
        };
    }

    private void ValidateProviderSecrets(EmailSettings settings, string provider)
    {
        if (!settings.Enabled)
        {
            return;
        }

        switch (provider)
        {
            case EmailProviders.SendGrid:
                if (string.IsNullOrWhiteSpace(UnprotectSecret(settings.ProtectedApiKey)))
                {
                    throw new InvalidOperationException(
                        "SendGrid API key is required when email is enabled (or the saved key cannot be decrypted).");
                }
                break;
            case EmailProviders.Mailgun:
                if (string.IsNullOrWhiteSpace(settings.MailgunDomain))
                {
                    throw new ArgumentException("Mailgun domain is required.", nameof(settings));
                }

                if (string.IsNullOrWhiteSpace(UnprotectSecret(settings.ProtectedApiKey)))
                {
                    throw new InvalidOperationException(
                        "Mailgun API key is required when email is enabled (or the saved key cannot be decrypted).");
                }
                break;
            default:
                if (string.IsNullOrWhiteSpace(settings.SmtpHost))
                {
                    throw new ArgumentException("SMTP host is required when email is enabled.", nameof(settings));
                }
                break;
        }
    }

    private async Task<EmailSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.EmailSettings
            .FirstOrDefaultAsync(item => item.Id == EmailSettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new EmailSettings
        {
            Id = EmailSettings.SingletonId,
            Provider = EmailProviders.Smtp,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.EmailSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private EmailSettingsDto ToDto(EmailSettings settings)
    {
        return new(
            settings.Enabled,
            EmailProviders.Normalize(settings.Provider),
            settings.FromAddress,
            settings.FromName,
            settings.SmtpHost,
            settings.SmtpPort,
            settings.UseSsl,
            settings.Username,
            !string.IsNullOrWhiteSpace(UnprotectSecret(settings.ProtectedPassword)),
            !string.IsNullOrWhiteSpace(UnprotectSecret(settings.ProtectedApiKey)),
            settings.MailgunDomain,
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
