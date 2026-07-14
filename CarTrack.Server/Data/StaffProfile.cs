namespace CarTrack.Server.Data;

public class StaffProfile : IAuditable
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string? EmployeeNumber { get; set; }

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public string? Branch { get; set; }

    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? AssignedDepartment { get; set; }

    public Guid? PositionId { get; set; }

    public Position? AssignedPosition { get; set; }

    public string EmploymentStatus { get; set; } = "Active";

    public DateOnly? WorkStartDate { get; set; }

    public string? ManagerUserId { get; set; }

    public ApplicationUser? Manager { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }
}
