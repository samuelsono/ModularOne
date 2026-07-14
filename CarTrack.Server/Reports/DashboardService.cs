using CarTrack.Server.CarTrack;
using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Reports;

public interface IDashboardService
{
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

public class DashboardService(ApplicationDbContext dbContext, IReportQueryService reportQueryService) : IDashboardService
{
    public async Task<DashboardsResponse> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dashboards = await dbContext.Dashboards
            .AsNoTracking()
            .Include(dashboard => dashboard.Sections)
            .ThenInclude(section => section.ReportPlacements)
            .ThenInclude(placement => placement.Report)
            .OrderBy(dashboard => dashboard.SortOrder)
            .ThenBy(dashboard => dashboard.Name)
            .ToListAsync(cancellationToken);

        var items = dashboards.Select(ReportMapper.ToSummaryDto).ToList();
        return new DashboardsResponse(items, items.Count);
    }

    public async Task<DashboardDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dashboard = await LoadDashboardGraphAsync(id, cancellationToken);
        return dashboard is null ? null : ReportMapper.ToDashboardDto(dashboard);
    }

    public async Task<DashboardDto?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.Dashboards
            .AsNoTracking()
            .Include(item => item.Sections)
            .ThenInclude(section => section.ReportPlacements)
            .ThenInclude(placement => placement.Report)
            .Where(item => item.IsDefault)
            .OrderBy(item => item.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        return dashboard is null ? null : ReportMapper.ToDashboardDto(dashboard);
    }

    public async Task<DashboardRenderDto?> GetDefaultRenderedAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.Dashboards
            .AsNoTracking()
            .Include(item => item.Sections)
            .ThenInclude(section => section.ReportPlacements)
            .ThenInclude(placement => placement.Report)
            .Where(item => item.IsDefault)
            .OrderBy(item => item.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        return dashboard is null ? null : await RenderDashboardAsync(dashboard, cancellationToken);
    }

    public async Task<DashboardRenderDto?> GetRenderedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dashboard = await LoadDashboardGraphAsync(id, cancellationToken);
        return dashboard is null ? null : await RenderDashboardAsync(dashboard, cancellationToken);
    }

    public async Task<DashboardDto> CreateAsync(
        SaveDashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IsDefault)
        {
            await ClearDefaultDashboardAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsDefault = request.IsDefault,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Dashboards.Add(dashboard);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ReportMapper.ToDashboardDto(dashboard);
    }

    public async Task<DashboardDto> UpdateAsync(
        Guid id,
        SaveDashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.Dashboards
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Dashboard '{id}' was not found.");

        if (request.IsDefault && !dashboard.IsDefault)
        {
            await ClearDefaultDashboardAsync(cancellationToken);
        }

        dashboard.Name = request.Name.Trim();
        dashboard.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        dashboard.IsDefault = request.IsDefault;
        dashboard.SortOrder = request.SortOrder;
        dashboard.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await LoadDashboardGraphAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Dashboard '{id}' was not found.");

        return ReportMapper.ToDashboardDto(reloaded);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.Dashboards
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (dashboard is null)
        {
            return;
        }

        dbContext.Dashboards.Remove(dashboard);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardSectionDto> CreateSectionAsync(
        Guid dashboardId,
        SaveDashboardSectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var dashboardExists = await dbContext.Dashboards
            .AnyAsync(item => item.Id == dashboardId, cancellationToken);

        if (!dashboardExists)
        {
            throw new KeyNotFoundException($"Dashboard '{dashboardId}' was not found.");
        }

        var existingSections = await dbContext.DashboardSections
            .Where(section => section.DashboardId == dashboardId)
            .ToListAsync(cancellationToken);

        await ValidateSectionParentAsync(
            dashboardId,
            request.ParentSectionId,
            sectionId: null,
            existingSections,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var section = new DashboardSection
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboardId,
            ParentSectionId = request.ParentSectionId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Subtitle = string.IsNullOrWhiteSpace(request.Subtitle) ? null : request.Subtitle.Trim(),
            LayoutDirection = request.LayoutDirection,
            Size = request.Size,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.DashboardSections.Add(section);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sectionsById = SectionHierarchy.IndexSections(existingSections.Append(section));
        return ReportMapper.ToSectionDto(section, sectionsById);
    }

    public async Task<DashboardSectionDto> UpdateSectionAsync(
        Guid sectionId,
        SaveDashboardSectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var section = await dbContext.DashboardSections
            .Include(item => item.ReportPlacements)
            .ThenInclude(placement => placement.Report)
            .FirstOrDefaultAsync(item => item.Id == sectionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Section '{sectionId}' was not found.");

        var existingSections = await dbContext.DashboardSections
            .Where(item => item.DashboardId == section.DashboardId)
            .ToListAsync(cancellationToken);

        await ValidateSectionParentAsync(
            section.DashboardId,
            request.ParentSectionId,
            sectionId,
            existingSections,
            cancellationToken);

        section.ParentSectionId = request.ParentSectionId;
        section.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        section.Subtitle = string.IsNullOrWhiteSpace(request.Subtitle) ? null : request.Subtitle.Trim();
        section.LayoutDirection = request.LayoutDirection;
        section.Size = request.Size;
        section.SortOrder = request.SortOrder;
        section.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var sectionsById = SectionHierarchy.IndexSections(existingSections);
        return ReportMapper.ToSectionDto(section, sectionsById);
    }

    public async Task DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await dbContext.DashboardSections
            .FirstOrDefaultAsync(item => item.Id == sectionId, cancellationToken);

        if (section is null)
        {
            return;
        }

        dbContext.DashboardSections.Remove(section);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardDto> ReorderLayoutAsync(
        Guid dashboardId,
        ReorderDashboardLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.Dashboards
            .Include(item => item.Sections)
            .ThenInclude(section => section.ReportPlacements)
            .ThenInclude(placement => placement.Report)
            .FirstOrDefaultAsync(item => item.Id == dashboardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dashboard '{dashboardId}' was not found.");

        var now = DateTimeOffset.UtcNow;

        foreach (var sectionLayout in request.Sections)
        {
            if (!Guid.TryParse(sectionLayout.SectionId, out var sectionId))
            {
                continue;
            }

            var section = dashboard.Sections.FirstOrDefault(item => item.Id == sectionId);
            if (section is null)
            {
                continue;
            }

            section.SortOrder = sectionLayout.SortOrder;
            section.UpdatedAt = now;

            for (var index = 0; index < sectionLayout.Items.Count; index++)
            {
                var layoutItem = sectionLayout.Items[index];
                var order = index + 1;

                if (layoutItem.ItemType.Equals("Report", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Guid.TryParse(layoutItem.ItemId, out var reportId))
                    {
                        continue;
                    }

                    var placement = section.ReportPlacements.FirstOrDefault(item => item.ReportId == reportId);
                    if (placement is null)
                    {
                        continue;
                    }

                    placement.SortOrder = order;
                    placement.UpdatedAt = now;
                    continue;
                }

                if (layoutItem.ItemType.Equals("Section", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Guid.TryParse(layoutItem.ItemId, out var childSectionId))
                    {
                        continue;
                    }

                    var childSection = dashboard.Sections.FirstOrDefault(item =>
                        item.Id == childSectionId && item.ParentSectionId == section.Id);

                    if (childSection is null)
                    {
                        continue;
                    }

                    childSection.SortOrder = order;
                    childSection.UpdatedAt = now;
                }
            }
        }

        dashboard.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await LoadDashboardGraphAsync(dashboardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dashboard '{dashboardId}' was not found.");

        return ReportMapper.ToDashboardDto(reloaded);
    }

    private async Task<Dashboard?> LoadDashboardGraphAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Dashboards
            .AsNoTracking()
            .Include(dashboard => dashboard.Sections)
            .ThenInclude(section => section.ReportPlacements)
            .ThenInclude(placement => placement.Report)
            .FirstOrDefaultAsync(dashboard => dashboard.Id == id, cancellationToken);

    private async Task ClearDefaultDashboardAsync(CancellationToken cancellationToken)
    {
        var currentDefaults = await dbContext.Dashboards
            .Where(dashboard => dashboard.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var dashboard in currentDefaults)
        {
            dashboard.IsDefault = false;
            dashboard.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task<DashboardRenderDto> RenderDashboardAsync(
        Dashboard dashboard,
        CancellationToken cancellationToken)
    {
        var sectionsById = SectionHierarchy.IndexSections(dashboard.Sections);
        var rootSections = SectionHierarchy.GetRootSections(dashboard.Sections);
        var renderedRoots = new List<DashboardSectionRenderDto>();

        foreach (var section in rootSections)
        {
            renderedRoots.Add(await RenderSectionAsync(section, dashboard.Sections, sectionsById, cancellationToken));
        }

        return new DashboardRenderDto(
            dashboard.Id.ToString(),
            dashboard.Name,
            dashboard.Description,
            dashboard.IsDefault,
            dashboard.SortOrder,
            renderedRoots,
            dashboard.UpdatedAt.ToString("O"));
    }

    private async Task<DashboardSectionRenderDto> RenderSectionAsync(
        DashboardSection section,
        IEnumerable<DashboardSection> allSections,
        IReadOnlyDictionary<Guid, DashboardSection> sectionsById,
        CancellationToken cancellationToken)
    {
        var reports = new List<ReportWithDataDto>();

        foreach (var placement in section.ReportPlacements
                     .Where(item => item.IsVisible)
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => item.Report.Name))
        {
            var reportDto = ReportMapper.ToDto(placement.Report, placement, section);
            ReportExecutionResultDto data;

            try
            {
                data = await reportQueryService.ExecuteAsync(placement.Report, cancellationToken);
            }
            catch (CarTrackApiException ex)
            {
                data = CreateFailedReportData(placement.Report, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                data = CreateFailedReportData(placement.Report, ex.Message);
            }

            reports.Add(new ReportWithDataDto(reportDto, data));
        }

        var childSections = SectionHierarchy.GetChildSections(allSections, section.Id);
        var renderedChildren = new List<DashboardSectionRenderDto>();

        foreach (var child in childSections)
        {
            renderedChildren.Add(await RenderSectionAsync(child, allSections, sectionsById, cancellationToken));
        }

        // Reports and child sections share sort order within a parent section.
        reports.Sort((left, right) => left.Report.SortOrder.CompareTo(right.Report.SortOrder));
        renderedChildren.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));

        return new DashboardSectionRenderDto(
            section.Id.ToString(),
            section.DashboardId.ToString(),
            section.ParentSectionId?.ToString(),
            SectionHierarchy.GetDepth(section, sectionsById),
            section.Title,
            section.Subtitle,
            section.LayoutDirection,
            section.Size,
            section.SortOrder,
            reports,
            renderedChildren,
            section.UpdatedAt.ToString("O"));
    }

    private static ReportExecutionResultDto CreateFailedReportData(ReportDefinition report, string message) =>
        new(
            report.Id.ToString(),
            report.ReportType,
            report.Name,
            ["Error"],
            [[message]],
            null);

    private static Task ValidateSectionParentAsync(
        Guid dashboardId,
        Guid? parentSectionId,
        Guid? sectionId,
        IReadOnlyList<DashboardSection> existingSections,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!parentSectionId.HasValue)
        {
            return Task.CompletedTask;
        }

        if (sectionId.HasValue && parentSectionId.Value == sectionId.Value)
        {
            throw new InvalidOperationException("A section cannot be its own parent.");
        }

        var sectionsById = SectionHierarchy.IndexSections(existingSections);
        if (!sectionsById.TryGetValue(parentSectionId.Value, out var parent))
        {
            throw new KeyNotFoundException($"Parent section '{parentSectionId}' was not found.");
        }

        if (parent.DashboardId != dashboardId)
        {
            throw new InvalidOperationException("Parent section must belong to the same dashboard.");
        }

        if (sectionId.HasValue && SectionHierarchy.IsDescendant(sectionId.Value, parentSectionId.Value, sectionsById))
        {
            throw new InvalidOperationException("A section cannot be nested under one of its descendants.");
        }

        var parentDepth = SectionHierarchy.GetDepth(parent, sectionsById);
        if (parentDepth >= SectionHierarchy.MaxDepth)
        {
            throw new InvalidOperationException($"Sections can be nested at most {SectionHierarchy.MaxDepth} levels deep.");
        }

        return Task.CompletedTask;
    }
}
