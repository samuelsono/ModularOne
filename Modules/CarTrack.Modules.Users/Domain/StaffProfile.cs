using CarTrack.Core;

namespace CarTrack.Modules.Users;

public class StaffProfile : IAuditable
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public string? EmployeeNumber { get; set; }

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public string? Branch { get; set; }

    /// <summary>Opaque CoreHr company id (no cross-module navigation).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Opaque CoreHr department id (no cross-module navigation).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Opaque CoreHr position id (no cross-module navigation).</summary>
    public Guid? PositionId { get; set; }

    public string EmploymentStatus { get; set; } = "Active";

    public DateOnly? WorkStartDate { get; set; }

    public string? ManagerUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }
}
