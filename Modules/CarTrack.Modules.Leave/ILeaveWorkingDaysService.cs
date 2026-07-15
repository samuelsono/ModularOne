namespace CarTrack.Modules.Leave;

public interface ILeaveWorkingDaysService
{
    Task<WorkingDaysResult> CalculateAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? branch,
        string? startDayPortion = null,
        string? endDayPortion = null,
        CancellationToken cancellationToken = default);
}
