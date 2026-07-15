namespace CarTrack.Modules.Reporting;

public interface IReportQueryService
{
    Task<ReportExecutionResultDto> ExecuteAsync(Guid reportId, CancellationToken cancellationToken = default);

    Task<ReportExecutionResultDto> ExecutePreviewAsync(
        SaveReportRequest request,
        CancellationToken cancellationToken = default);
}
