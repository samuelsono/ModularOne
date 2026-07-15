namespace CarTrack.Modules.Leave;

public class LeaveBalance
{
    public Guid Id { get; set; }

    public required string UserId { get; set; }

    public Guid LeaveTypeId { get; set; }

    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly CycleStart { get; set; }

    public DateOnly CycleEnd { get; set; }

    public decimal Allocated { get; set; }

    public decimal Used { get; set; }

    public decimal Pending { get; set; }

    public decimal Adjusted { get; set; }

    public int? LastAccruedYearMonth { get; set; }
}
