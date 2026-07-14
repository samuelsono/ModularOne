namespace CarTrack.Server.Reports;

public record ReportFilterDto(
    string Field,
    string Operator,
    string Value);

public record ReportPlacementDto(
    string Id,
    string SectionId,
    string? DashboardId,
    string? DashboardName,
    string? SectionTitle,
    int SortOrder,
    string Size,
    bool IsVisible);

public record ReportDto(
    string Id,
    string? SectionId,
    string? DashboardId,
    string? DashboardName,
    string? SectionTitle,
    string Name,
    string? Description,
    string ReportType,
    string Size,
    int SortOrder,
    bool IsVisible,
    string TargetTable,
    string AggregateFunction,
    string? AggregateField,
    IReadOnlyList<string> GroupByColumns,
    IReadOnlyList<ReportFilterDto> Filters,
    bool ComparisonEnabled,
    string? ChartOptionsJson,
    IReadOnlyList<ReportPlacementDto> Placements,
    string UpdatedAt);

public record ReportsResponse(
    IReadOnlyList<ReportDto> Items,
    int Total);

public record SaveReportPlacementRequest(
    Guid SectionId,
    int SortOrder,
    string Size,
    bool IsVisible);

public record SaveReportRequest(
    string Name,
    string? Description,
    string ReportType,
    string Size,
    int SortOrder,
    bool IsVisible,
    string TargetTable,
    string AggregateFunction,
    string? AggregateField,
    IReadOnlyList<string>? GroupByColumns,
    IReadOnlyList<ReportFilterDto>? Filters,
    bool ComparisonEnabled,
    string? ChartOptionsJson,
    Guid? SectionId,
    IReadOnlyList<SaveReportPlacementRequest>? Placements);

public record DashboardSectionDto(
    string Id,
    string DashboardId,
    string? ParentSectionId,
    int Depth,
    string? Title,
    string? Subtitle,
    string LayoutDirection,
    string Size,
    int SortOrder,
    IReadOnlyList<ReportDto> Reports,
    string UpdatedAt);

public record DashboardDto(
    string Id,
    string Name,
    string? Description,
    bool IsDefault,
    int SortOrder,
    IReadOnlyList<DashboardSectionDto> Sections,
    string UpdatedAt);

public record DashboardsResponse(
    IReadOnlyList<DashboardSummaryDto> Items,
    int Total);

public record DashboardSummaryDto(
    string Id,
    string Name,
    string? Description,
    bool IsDefault,
    int SortOrder,
    int SectionCount,
    int ReportCount,
    string UpdatedAt);

public record SaveDashboardRequest(
    string Name,
    string? Description,
    bool IsDefault,
    int SortOrder);

public record SaveDashboardSectionRequest(
    string? Title,
    string? Subtitle,
    string LayoutDirection,
    string Size,
    int SortOrder,
    Guid? ParentSectionId);

public record ReorderLayoutItemDto(
    string ItemType,
    string ItemId);

public record ReorderSectionLayoutDto(
    string SectionId,
    int SortOrder,
    IReadOnlyList<ReorderLayoutItemDto> Items);

public record ReorderDashboardLayoutRequest(
    IReadOnlyList<ReorderSectionLayoutDto> Sections);

public record ReportColumnMetadataDto(
    string Name,
    string Label,
    string Kind,
    bool IsGroupable,
    bool IsAggregatable);

public record ReportTableMetadataDto(
    string Name,
    string Label,
    IReadOnlyList<ReportColumnMetadataDto> Columns,
    IReadOnlyList<string> AllowedAggregates);

public record ReportTypeMetadataDto(
    string Name,
    int MaxGroupByColumns);

public record ReportMetadataResponse(
    IReadOnlyList<ReportTableMetadataDto> Tables,
    IReadOnlyList<string> ReportTypes,
    IReadOnlyList<string> ReportSizes,
    IReadOnlyList<string> LayoutDirections,
    IReadOnlyList<string> AggregateFunctions,
    IReadOnlyList<ReportTypeMetadataDto> ReportTypeRules,
    IReadOnlyList<string> ApiResources);
