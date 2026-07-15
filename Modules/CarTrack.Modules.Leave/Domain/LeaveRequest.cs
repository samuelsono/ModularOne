using CarTrack.Core;

namespace CarTrack.Modules.Leave;

public class LeaveRequest : IAuditable
{
    public Guid Id { get; set; }

    public required string RequesterUserId { get; set; }

    public required string ManagerUserId { get; set; }

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

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? DecidedByUserId { get; set; }
}

public static class ApprovalStatuses
{
    public const string Pending = "Pending";

    public const string Approved = "Approved";

    public const string Rejected = "Rejected";

    public const string Cancelled = "Cancelled";
}
