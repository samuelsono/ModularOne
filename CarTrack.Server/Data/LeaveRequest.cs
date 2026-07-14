namespace CarTrack.Server.Data;

public class LeaveRequest : IAuditable
{
    public Guid Id { get; set; }

    public required string RequesterUserId { get; set; }

    public ApplicationUser Requester { get; set; } = null!;

    public required string ManagerUserId { get; set; }

    public ApplicationUser Manager { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }

    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal WorkingDays { get; set; }

    public string StartDayPortion { get; set; } = "Full";

    public string EndDayPortion { get; set; } = "Full";

    public string Status { get; set; } = ApprovalStatuses.Pending;

    public string? RequesterBranch { get; set; }

    public string? Notes { get; set; }

    public string? DocumentPath { get; set; }

    public string? DocumentFileName { get; set; }

    public string? DocumentContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? DecidedByUserId { get; set; }
}
