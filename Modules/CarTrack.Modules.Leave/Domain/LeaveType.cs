using CarTrack.Core;

namespace CarTrack.Modules.Leave;

public class LeaveType : IAuditable
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    public string Color { get; set; } = "#0078D4";

    public bool IsPaid { get; set; } = true;

    public bool DeductsBalance { get; set; } = true;

    public bool RequiresDocument { get; set; }

    public bool AllowHalfDay { get; set; } = true;

    public string AccrualMethod { get; set; } = "Upfront";

    public decimal? AnnualEntitlement { get; set; }

    public int? MaxConsecutiveDays { get; set; }

    public int MinNoticeDays { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ICollection<LeaveRequest> Requests { get; set; } = [];
}
