using CarTrack.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace CarTrack.Modules.Users;

/// <summary>
/// Sends mail using DB email settings when enabled; otherwise appsettings SMTP; otherwise logs.
/// </summary>
public sealed class ConfigurableEmailService(
    IEmailRuntimeSettingsProvider runtimeSettingsProvider,
    IEmailDispatcher emailDispatcher,
    IOptions<EmailOptions> emailOptions,
    ILogger<ConfigurableEmailService> logger) : IEmailService
{
    private readonly EmailOptions _fallback = emailOptions.Value;

    public async Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var runtime = await runtimeSettingsProvider.GetAsync(cancellationToken);
        if (runtime is not null)
        {
            await emailDispatcher.SendAsync(runtime, toAddress, subject, htmlBody, cancellationToken);
            return;
        }

        if (_fallback.Enabled && !string.IsNullOrWhiteSpace(_fallback.SmtpHost))
        {
            var config = new EmailRuntimeConfig(
                EmailProviderNames.Smtp,
                _fallback.FromAddress,
                _fallback.FromName,
                SmtpHost: _fallback.SmtpHost,
                SmtpPort: _fallback.SmtpPort > 0 ? _fallback.SmtpPort : 587,
                UseSsl: _fallback.UseSsl,
                Username: _fallback.Username,
                Password: _fallback.Password);

            await emailDispatcher.SendAsync(config, toAddress, subject, htmlBody, cancellationToken);
            return;
        }

        logger.LogInformation(
            "Email (dev log) To={To} Subject={Subject} Body={Body}",
            toAddress,
            subject,
            htmlBody);
    }
}
