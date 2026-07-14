namespace CarTrack.Server.Data;

public class Position : IAuditable
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public required string Name { get; set; }

    public required string Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<StaffProfile> StaffProfiles { get; set; } = [];
}
