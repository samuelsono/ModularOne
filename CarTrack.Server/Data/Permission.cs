namespace CarTrack.Server.Data;

public class Permission
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string ModuleSlug { get; set; }

    public required string SubmoduleSlug { get; set; }

    public required string Action { get; set; }

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
