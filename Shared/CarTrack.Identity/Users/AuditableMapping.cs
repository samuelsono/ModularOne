namespace CarTrack.Server.Data;

/// <summary>
/// Display/timestamp helpers for auditable DTOs. Lives in CarTrack.Identity with
/// the ApplicationUser type; keeps the historical namespace for call-site compatibility.
/// </summary>
public static class AuditableMapping
{
    public static string? UserDisplayName(ApplicationUser? user) =>
        user?.DisplayName ?? user?.UserName ?? user?.Email;

    public static string FormatTimestamp(DateTimeOffset value) => value.ToString("O");

    public static string? FormatTimestamp(DateTimeOffset? value) =>
        value?.ToString("O");
}
