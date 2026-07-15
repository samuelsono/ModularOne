namespace CarTrack.Modules.Leave;

public interface ILeaveCalendarService
{
    Task<LeaveCalendarResponse> GetCalendarAsync(
        string viewerUserId,
        LeaveCalendarFilters filters,
        CancellationToken cancellationToken = default);
}
