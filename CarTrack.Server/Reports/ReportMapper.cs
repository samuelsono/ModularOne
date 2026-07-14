using System.Text.Json;
using CarTrack.Server.Data;

namespace CarTrack.Server.Reports;

public static class ReportMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ReportPlacementDto ToPlacementDto(ReportPlacement placement)
    {
        var section = placement.Section;

        return new ReportPlacementDto(
            placement.Id.ToString(),
            placement.SectionId.ToString(),
            section?.DashboardId.ToString(),
            section?.Dashboard?.Name,
            section?.Title,
            placement.SortOrder,
            placement.Size,
            placement.IsVisible);
    }

    public static ReportDto ToDto(ReportDefinition report, ReportPlacement? placement = null)
    {
        var placements = report.Placements
            .OrderBy(item => item.Section?.Dashboard?.SortOrder ?? int.MaxValue)
            .ThenBy(item => item.Section?.SortOrder ?? int.MaxValue)
            .ThenBy(item => item.SortOrder)
            .Select(ToPlacementDto)
            .ToList();

        var primary = placement ?? report.Placements
            .OrderBy(item => item.SortOrder)
            .FirstOrDefault();

        return new ReportDto(
            report.Id.ToString(),
            primary?.SectionId.ToString(),
            primary?.Section?.DashboardId.ToString(),
            primary?.Section?.Dashboard?.Name,
            primary?.Section?.Title,
            report.Name,
            report.Description,
            report.ReportType,
            report.Size,
            primary?.SortOrder ?? 1,
            primary?.IsVisible ?? true,
            report.TargetTable,
            report.AggregateFunction,
            report.AggregateField,
            DeserializeList(report.GroupByColumnsJson),
            DeserializeFilters(report.FiltersJson),
            report.ComparisonEnabled,
            report.ChartOptionsJson,
            placements,
            report.UpdatedAt.ToString("O"));
    }

    public static ReportDto ToDto(ReportDefinition report, ReportPlacement placement, DashboardSection section) =>
        ToDto(report, placement) with
        {
            SectionId = placement.SectionId.ToString(),
            DashboardId = section.DashboardId.ToString(),
            DashboardName = section.Dashboard?.Name,
            SectionTitle = section.Title,
            SortOrder = placement.SortOrder,
            IsVisible = placement.IsVisible,
            Size = placement.Size,
        };

    public static DashboardSectionDto ToSectionDto(
        DashboardSection section,
        IReadOnlyDictionary<Guid, DashboardSection> sectionsById) =>
        new(
            section.Id.ToString(),
            section.DashboardId.ToString(),
            section.ParentSectionId?.ToString(),
            SectionHierarchy.GetDepth(section, sectionsById),
            section.Title,
            section.Subtitle,
            section.LayoutDirection,
            section.Size,
            section.SortOrder,
            section.ReportPlacements
                .Where(placement => placement.Report is not null)
                .OrderBy(placement => placement.SortOrder)
                .ThenBy(placement => placement.Report!.Name)
                .Select(placement => ToDto(placement.Report!, placement, section))
                .ToList(),
            section.UpdatedAt.ToString("O"));

    public static DashboardDto ToDashboardDto(Dashboard dashboard)
    {
        var sectionsById = SectionHierarchy.IndexSections(dashboard.Sections);

        return new(
            dashboard.Id.ToString(),
            dashboard.Name,
            dashboard.Description,
            dashboard.IsDefault,
            dashboard.SortOrder,
            dashboard.Sections
                .OrderBy(section => section.SortOrder)
                .ThenBy(section => section.Title)
                .Select(section => ToSectionDto(section, sectionsById))
                .ToList(),
            dashboard.UpdatedAt.ToString("O"));
    }

    public static DashboardSummaryDto ToSummaryDto(Dashboard dashboard) =>
        new(
            dashboard.Id.ToString(),
            dashboard.Name,
            dashboard.Description,
            dashboard.IsDefault,
            dashboard.SortOrder,
            dashboard.Sections.Count,
            dashboard.Sections.Sum(section => section.ReportPlacements.Count),
            dashboard.UpdatedAt.ToString("O"));

    public static void ApplyRequest(ReportDefinition report, SaveReportRequest request)
    {
        report.Name = request.Name.Trim();
        report.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        report.ReportType = request.ReportType;
        report.Size = request.Size;
        report.TargetTable = request.TargetTable;
        report.AggregateFunction = request.AggregateFunction;
        report.AggregateField = string.IsNullOrWhiteSpace(request.AggregateField)
            ? null
            : request.AggregateField.Trim();
        report.GroupByColumnsJson = SerializeList(request.GroupByColumns ?? []);
        report.FiltersJson = SerializeFilters(request.Filters ?? []);
        report.ComparisonEnabled = request.ComparisonEnabled;
        report.ChartOptionsJson = string.IsNullOrWhiteSpace(request.ChartOptionsJson)
            ? null
            : request.ChartOptionsJson.Trim();
    }

    public static IReadOnlyList<SaveReportPlacementRequest> ResolvePlacements(SaveReportRequest request)
    {
        if (request.Placements is { Count: > 0 })
        {
            return request.Placements;
        }

        if (request.SectionId is not null && request.SectionId != Guid.Empty)
        {
            return
            [
                new SaveReportPlacementRequest(
                    request.SectionId.Value,
                    request.SortOrder,
                    request.Size,
                    request.IsVisible),
            ];
        }

        return [];
    }

    public static string SerializeList(IReadOnlyList<string> values) =>
        JsonSerializer.Serialize(values, JsonOptions);

    public static string SerializeFilters(IReadOnlyList<ReportFilterDto> filters) =>
        JsonSerializer.Serialize(filters, JsonOptions);

    public static IReadOnlyList<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    public static IReadOnlyList<ReportFilterDto> DeserializeFilters(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<ReportFilterDto>>(json, JsonOptions) ?? [];
    }
}
