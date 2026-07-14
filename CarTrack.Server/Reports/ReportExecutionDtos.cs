namespace CarTrack.Server.Reports;

public record ReportMetricDto(
    double Value,
    double? ComparisonValue,
    double? ChangePercent);

public record ReportExecutionResultDto(
    string ReportId,
    string ReportType,
    string Name,
    IReadOnlyList<string> Columns,
    IReadOnlyList<object?[]> Rows,
    ReportMetricDto? Metric);

public record ReportWithDataDto(
    ReportDto Report,
    ReportExecutionResultDto Data);

public record DashboardSectionRenderDto(
    string Id,
    string DashboardId,
    string? ParentSectionId,
    int Depth,
    string? Title,
    string? Subtitle,
    string LayoutDirection,
    string Size,
    int SortOrder,
    IReadOnlyList<ReportWithDataDto> Reports,
    IReadOnlyList<DashboardSectionRenderDto> ChildSections,
    string UpdatedAt);

public record DashboardRenderDto(
    string Id,
    string Name,
    string? Description,
    bool IsDefault,
    int SortOrder,
    IReadOnlyList<DashboardSectionRenderDto> Sections,
    string UpdatedAt);
