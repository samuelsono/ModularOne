namespace CarTrack.Modules.Leave;

public interface ILeaveConfigurationService
{
    Task<IReadOnlyList<LeaveTypeDto>> GetActiveTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveTypeDto>> GetAdminTypesAsync(CancellationToken cancellationToken = default);

    Task<LeaveTypeDto> CreateTypeAsync(
        SaveLeaveTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaveTypeDto?> UpdateTypeAsync(
        Guid id,
        SaveLeaveTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicHolidayDto>> GetAdminHolidaysAsync(
        int? year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicHolidayDto>> GetUpcomingHolidaysAsync(
        int untilYear,
        CancellationToken cancellationToken = default);

    Task<PublicHolidayDto> CreateHolidayAsync(
        SavePublicHolidayRequest request,
        CancellationToken cancellationToken = default);

    Task<PublicHolidayDto?> UpdateHolidayAsync(
        Guid id,
        SavePublicHolidayRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteHolidayAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> SyncHolidaysAsync(
        int? year,
        CancellationToken cancellationToken = default);
}
