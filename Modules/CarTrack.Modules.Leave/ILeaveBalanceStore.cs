
namespace CarTrack.Modules.Leave;

public interface ILeaveBalanceStore
{
    Task<LeaveBalance> GetOrCreateBalanceAsync(
        string userId,
        Guid leaveTypeId,
        DateOnly cycleStart,
        DateOnly cycleEnd,
        CancellationToken cancellationToken = default);
}
