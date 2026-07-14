using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Reports;

public interface IReportService
{
    ReportMetadataResponse GetMetadata();

    Task<ReportsResponse> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ReportDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReportDto> CreateAsync(SaveReportRequest request, CancellationToken cancellationToken = default);

    Task<ReportDto> UpdateAsync(Guid id, SaveReportRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ReportService(ApplicationDbContext dbContext) : IReportService
{
    public ReportMetadataResponse GetMetadata() =>
        new(
            ReportTableRegistry.GetAllTables()
                .Select(table => new ReportTableMetadataDto(
                    table.Name,
                    table.Label,
                    table.Columns.Select(column => new ReportColumnMetadataDto(
                        column.Name,
                        column.Label,
                        column.Kind.ToString(),
                        column.IsGroupable,
                        column.IsAggregatable)).ToList(),
                    table.AllowedAggregates))
                .ToList(),
            ReportTypes.All,
            ReportSizes.All,
            LayoutDirections.All,
            AggregateFunctions.All,
            ReportTypes.All
                .Select(type => new ReportTypeMetadataDto(type, ReportTypes.MaxGroupByColumns(type)))
                .ToList(),
            ApiResources.All);

    public async Task<ReportsResponse> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reports = await dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(report => report.Placements)
            .ThenInclude(placement => placement.Section)
            .ThenInclude(section => section.Dashboard)
            .OrderBy(report => report.Name)
            .ToListAsync(cancellationToken);

        var items = reports.Select(report => ReportMapper.ToDto(report)).ToList();
        return new ReportsResponse(items, items.Count);
    }

    public async Task<ReportDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await LoadReportGraphAsync(id, cancellationToken);
        return report is null ? null : ReportMapper.ToDto(report);
    }

    public async Task<ReportDto> CreateAsync(
        SaveReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var placementRequests = ReportMapper.ResolvePlacements(request);

        if (placementRequests.Count > 0)
        {
            await ValidatePlacementSectionsAsync(placementRequests, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var report = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };

        ReportMapper.ApplyRequest(report, request);
        dbContext.ReportDefinitions.Add(report);

        foreach (var placementRequest in placementRequests)
        {
            report.Placements.Add(new ReportPlacement
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                SectionId = placementRequest.SectionId,
                SortOrder = placementRequest.SortOrder,
                Size = placementRequest.Size,
                IsVisible = placementRequest.IsVisible,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(report.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload created report.");
    }

    public async Task<ReportDto> UpdateAsync(
        Guid id,
        SaveReportRequest request,
        CancellationToken cancellationToken = default)
    {
        // Load placements only — including Section pulls in every placement on that
        // section (other reports), which breaks relationship fixup when adding new ones.
        var report = await dbContext.ReportDefinitions
            .Include(item => item.Placements)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Report '{id}' was not found.");

        ReportMapper.ApplyRequest(report, request);
        report.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Placements is not null)
        {
            if (request.Placements.Count > 0)
            {
                await ValidatePlacementSectionsAsync(request.Placements, cancellationToken);
            }

            SyncPlacements(report, request.Placements);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload updated report.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await dbContext.ReportDefinitions
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (report is null)
        {
            return;
        }

        dbContext.ReportDefinitions.Remove(report);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReportDefinition?> LoadReportGraphAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.ReportDefinitions
            .AsNoTracking()
            .Include(report => report.Placements)
            .ThenInclude(placement => placement.Section)
            .ThenInclude(section => section.Dashboard)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    private async Task ValidatePlacementSectionsAsync(
        IReadOnlyList<SaveReportPlacementRequest> placements,
        CancellationToken cancellationToken)
    {
        var sectionIds = placements.Select(placement => placement.SectionId).Distinct().ToList();
        var existingCount = await dbContext.DashboardSections
            .CountAsync(section => sectionIds.Contains(section.Id), cancellationToken);

        if (existingCount != sectionIds.Count)
        {
            throw new KeyNotFoundException("One or more placement sections were not found.");
        }

        foreach (var placement in placements)
        {
            if (!ReportSizes.All.Contains(placement.Size, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid placement size '{placement.Size}'.");
            }
        }
    }

    private void SyncPlacements(
        ReportDefinition report,
        IReadOnlyList<SaveReportPlacementRequest> placementRequests)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedRequests = placementRequests
            .GroupBy(placement => placement.SectionId)
            .Select(group => group.Last())
            .ToList();

        var requestedSectionIds = normalizedRequests
            .Select(placement => placement.SectionId)
            .ToHashSet();

        var toRemove = report.Placements
            .Where(placement => !requestedSectionIds.Contains(placement.SectionId))
            .ToList();

        foreach (var placement in toRemove)
        {
            report.Placements.Remove(placement);
            dbContext.ReportPlacements.Remove(placement);
        }

        foreach (var placementRequest in normalizedRequests)
        {
            var existing = report.Placements
                .FirstOrDefault(placement => placement.SectionId == placementRequest.SectionId);

            if (existing is null)
            {
                var placement = new ReportPlacement
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    SectionId = placementRequest.SectionId,
                    SortOrder = placementRequest.SortOrder,
                    Size = placementRequest.Size,
                    IsVisible = placementRequest.IsVisible,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                report.Placements.Add(placement);
                dbContext.ReportPlacements.Add(placement);
            }
            else
            {
                existing.SortOrder = placementRequest.SortOrder;
                existing.Size = placementRequest.Size;
                existing.IsVisible = placementRequest.IsVisible;
                existing.UpdatedAt = now;
            }
        }
    }
}
