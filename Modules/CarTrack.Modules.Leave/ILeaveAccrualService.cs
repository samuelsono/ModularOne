namespace CarTrack.Modules.Leave;

public interface ILeaveAccrualService
{
    Task<int> RunMonthlyAccrualAsync(CancellationToken cancellationToken = default);
}
