namespace CarTrack.Server.Data;

public class RolePermission
{
    public Guid Id { get; set; }

    public required string RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
