namespace CarTrack.Server.Data;

public class DriverProfileLink
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid DriverId { get; set; }

    public Driver Driver { get; set; } = null!;
}
