namespace CarTrack.Modules.Reporting;

public interface IDashboardService
{
    IReadOnlyDictionary<string, string[]>? ValidateDashboard(SaveDashboardRequest request);

    IReadOnlyDictionary<string, string[]>? ValidateSection(SaveDashboardSectionRequest request);

    Task<DashboardsResponse> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DashboardDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DashboardDto?> GetDefaultAsync(CancellationToken cancellationToken = default);

    Task<DashboardRenderDto?> GetDefaultRenderedAsync(CancellationToken cancellationToken = default);

    Task<DashboardRenderDto?> GetRenderedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DashboardDto> CreateAsync(SaveDashboardRequest request, CancellationToken cancellationToken = default);

    Task<DashboardDto> UpdateAsync(Guid id, SaveDashboardRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DashboardSectionDto> CreateSectionAsync(
        Guid dashboardId,
        SaveDashboardSectionRequest request,
        CancellationToken cancellationToken = default);

    Task<DashboardSectionDto> UpdateSectionAsync(
        Guid sectionId,
        SaveDashboardSectionRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task<DashboardDto> ReorderLayoutAsync(
        Guid dashboardId,
        ReorderDashboardLayoutRequest request,
        CancellationToken cancellationToken = default);
}
