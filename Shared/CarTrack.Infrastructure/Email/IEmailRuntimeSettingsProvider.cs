namespace CarTrack.Infrastructure.Email;

public static class EmailProviderNames
{
    public const string Smtp = "Smtp";
    public const string SendGrid = "SendGrid";
    public const string Mailgun = "Mailgun";
}

/// <summary>Resolved outbound email configuration used at send time.</summary>
public sealed record EmailRuntimeConfig(
    string Provider,
    string FromAddress,
    string FromName,
    string? SmtpHost = null,
    int SmtpPort = 587,
    bool UseSsl = true,
    string? Username = null,
    string? Password = null,
    string? ApiKey = null,
    string? MailgunDomain = null);

public interface IEmailRuntimeSettingsProvider
{
    /// <summary>
    /// Returns DB-backed email settings when enabled and complete; otherwise null
    /// (caller should fall back to appsettings or logging).
    /// </summary>
    Task<EmailRuntimeConfig?> GetAsync(CancellationToken cancellationToken = default);
}
