using Microsoft.AspNetCore.Identity;

namespace CarTrack.Server.Data;

/// <summary>
/// Shared identity user. Lives in CarTrack.Identity; keeps the
/// <c>CarTrack.Server.Data</c> namespace so existing EF migrations remain valid.
/// StaffProfile / DriverProfileLink navigations are configured from those
/// entities (no inverse props here — avoids Identity→Server circular refs).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public bool MustChangePassword { get; set; }

    public DateTimeOffset? InvitePendingAt { get; set; }
}
