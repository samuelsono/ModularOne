namespace CarTrack.Modules.Users;

public class DriverProfileLink
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    /// <summary>Opaque Fleet driver id (no cross-module navigation).</summary>
    public Guid DriverId { get; set; }
}
