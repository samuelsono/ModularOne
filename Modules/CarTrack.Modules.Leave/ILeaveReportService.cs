namespace CarTrack.Modules.Leave;

public interface ILeaveReportService
{
    Task<LeaveReportSummaryDto> GetSummaryAsync(
        string viewerUserId,
        int? year,
        string? department,
        string? branch,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveHistoryRowDto>> GetHistoryAsync(
        string viewerUserId,
        LeaveHistoryFilters filters,
        CancellationToken cancellationToken = default);

    string BuildHistoryCsv(IReadOnlyList<LeaveHistoryRowDto> rows);

    Task<IReadOnlyList<LeaveLiabilityRowDto>> GetLiabilityAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetPendingReportAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default);
}
