namespace CarTrack.Server.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CarTrack";

    public string Audience { get; set; } = "CarTrack";

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public int RefreshTokenRememberMeDays { get; set; } = 30;

    public int MfaChallengeMinutes { get; set; } = 5;
}
