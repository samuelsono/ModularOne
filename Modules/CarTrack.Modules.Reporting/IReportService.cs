namespace CarTrack.Modules.Reporting;

public interface IReportService
{
    ReportMetadataResponse GetMetadata();

    /// <summary>
    /// Validates a save request using Host-side table/registry rules (B6).
    /// Returns field errors, or null when valid.
    /// </summary>
    IReadOnlyDictionary<string, string[]>? Validate(SaveReportRequest request);

    Task<ReportsResponse> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ReportDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReportDto> CreateAsync(SaveReportRequest request, CancellationToken cancellationToken = default);

    Task<ReportDto> UpdateAsync(Guid id, SaveReportRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
