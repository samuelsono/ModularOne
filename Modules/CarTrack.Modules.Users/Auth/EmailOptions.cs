namespace CarTrack.Modules.Users;

public class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }

    public string FromAddress { get; set; } = "noreply@cartrack.local";

    public string FromName { get; set; } = "TalisTrack";

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }
}
