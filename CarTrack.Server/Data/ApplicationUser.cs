using Microsoft.AspNetCore.Identity;

namespace CarTrack.Server.Data;

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

    public StaffProfile? StaffProfile { get; set; }

    public DriverProfileLink? DriverLink { get; set; }
}
