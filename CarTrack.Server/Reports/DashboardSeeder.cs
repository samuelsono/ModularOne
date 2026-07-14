using CarTrack.Server.Data;
using CarTrack.Server.Reports;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Reports;

public static class DashboardSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Dashboards.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Name = "Fleet Overview",
            Description = "Key fleet metrics and breakdowns",
            IsDefault = true,
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var metricsSection = new DashboardSection
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboard.Id,
            Title = "Fleet Summary",
            Subtitle = "Live counts from your vehicle and driver data",
            LayoutDirection = LayoutDirections.Row,
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var chartsSection = new DashboardSection
        {
            Id = Guid.NewGuid(),
            DashboardId = dashboard.Id,
            Title = "Breakdowns",
            LayoutDirection = LayoutDirections.Row,
            SortOrder = 2,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var seededReports = new (Guid SectionId, ReportDefinition Report, int SortOrder)[]
        {
            (metricsSection.Id, CreateReport("Total Vehicles", ReportTypes.MetricCard, ReportSizes.Small, TargetTables.Vehicles, AggregateFunctions.Count, null, [], now), 1),
            (metricsSection.Id, CreateReport("Active Vehicles", ReportTypes.MetricCard, ReportSizes.Small, TargetTables.Vehicles, AggregateFunctions.Count, null, [], now, filters: [new ReportFilterDto("IgnitionStatus", "equals", "on")]), 2),
            (metricsSection.Id, CreateReport("Total Drivers", ReportTypes.MetricCard, ReportSizes.Small, TargetTables.Drivers, AggregateFunctions.Count, null, [], now), 3),
            (chartsSection.Id, CreateReport("Vehicles by Make", ReportTypes.Column, ReportSizes.Large, TargetTables.Vehicles, AggregateFunctions.Count, null, ["Make"], now), 1),
            (chartsSection.Id, CreateReport("Drivers by Department", ReportTypes.Donut, ReportSizes.Medium, TargetTables.Drivers, AggregateFunctions.Count, null, ["Department"], now), 2),
            (chartsSection.Id, CreateReport("Vehicles by Fuel Type", ReportTypes.Pie, ReportSizes.Medium, TargetTables.Vehicles, AggregateFunctions.Count, null, ["FuelType"], now), 3),
        };

        var reports = new List<ReportDefinition>();
        var placements = new List<ReportPlacement>();

        foreach (var (sectionId, report, sortOrder) in seededReports)
        {
            reports.Add(report);
            placements.Add(new ReportPlacement
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                SectionId = sectionId,
                SortOrder = sortOrder,
                Size = report.Size,
                IsVisible = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        dbContext.Dashboards.Add(dashboard);
        dbContext.DashboardSections.AddRange(metricsSection, chartsSection);
        dbContext.ReportDefinitions.AddRange(reports);
        dbContext.ReportPlacements.AddRange(placements);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ReportDefinition CreateReport(
        string name,
        string reportType,
        string size,
        string targetTable,
        string aggregateFunction,
        string? aggregateField,
        IReadOnlyList<string> groupByColumns,
        DateTimeOffset now,
        IReadOnlyList<ReportFilterDto>? filters = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ReportType = reportType,
            Size = size,
            TargetTable = targetTable,
            AggregateFunction = aggregateFunction,
            AggregateField = aggregateField,
            GroupByColumnsJson = ReportMapper.SerializeList(groupByColumns),
            FiltersJson = ReportMapper.SerializeFilters(filters ?? []),
            ComparisonEnabled = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
