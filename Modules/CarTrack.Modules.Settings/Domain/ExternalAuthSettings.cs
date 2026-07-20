namespace CarTrack.Modules.Settings;

/// <summary>OAuth client credentials for a social/enterprise login provider.</summary>
public class ExternalAuthSettings
{
    public required string Provider { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string? ProtectedClientSecret { get; set; }

    /// <summary>Microsoft Entra tenant (`common`, `organizations`, or a tenant GUID).</summary>
    public string? TenantId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
