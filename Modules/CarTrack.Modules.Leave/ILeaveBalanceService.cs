namespace CarTrack.Modules.Leave;

public interface ILeaveBalanceService
{
    Task<IReadOnlyList<LeaveBalanceDto>> GetMyBalancesAsync(
        string userId,
        int? year = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveBalanceDto>> GetUserBalancesAsync(
        string userId,
        int? year = null,
        CancellationToken cancellationToken = default);

    Task<LeaveBalanceDto> AdjustBalanceAsync(
        AdjustLeaveBalanceRequest request,
        CancellationToken cancellationToken = default);
}
