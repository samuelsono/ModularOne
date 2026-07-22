namespace CarTrack.Modules.Settings;

/// <summary>Singleton outbound email settings (Id is always 1).</summary>
public class EmailSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public bool Enabled { get; set; }

    /// <summary>Smtp | SendGrid | Mailgun</summary>
    public string Provider { get; set; } = EmailProviders.Smtp;

    public string FromAddress { get; set; } = "noreply@cartrack.local";

    public string FromName { get; set; } = "TalisTrack";

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? Username { get; set; }

    public string? ProtectedPassword { get; set; }

    public string? ProtectedApiKey { get; set; }

    public string? MailgunDomain { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public static class EmailProviders
{
    public const string Smtp = "Smtp";
    public const string SendGrid = "SendGrid";
    public const string Mailgun = "Mailgun";

    public static bool IsKnown(string? provider) =>
        string.Equals(provider, Smtp, StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider, SendGrid, StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider, Mailgun, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? provider) =>
        provider?.Trim() switch
        {
            var value when string.Equals(value, SendGrid, StringComparison.OrdinalIgnoreCase) => SendGrid,
            var value when string.Equals(value, Mailgun, StringComparison.OrdinalIgnoreCase) => Mailgun,
            _ => Smtp,
        };
}
