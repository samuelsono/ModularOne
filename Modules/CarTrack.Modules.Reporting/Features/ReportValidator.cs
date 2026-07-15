namespace CarTrack.Modules.Reporting;

public static class ReportValidator
{
    public static IReadOnlyDictionary<string, string[]>? ValidateReport(SaveReportRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Report name is required."];
        }

        if (!ReportTypes.All.Contains(request.ReportType, StringComparer.OrdinalIgnoreCase))
        {
            errors["reportType"] = ["Invalid report type."];
            return errors;
        }

        if (!ReportSizes.All.Contains(request.Size, StringComparer.OrdinalIgnoreCase))
        {
            errors["size"] = ["Invalid report size."];
        }

        if (request.ReportType.Equals(ReportTypes.Map, StringComparison.OrdinalIgnoreCase))
        {
            return errors.Count > 0 ? errors : null;
        }

        if (request.ReportType.Equals(ReportTypes.ApiTable, StringComparison.OrdinalIgnoreCase))
        {
            ValidateApiTable(request, errors);
            return errors.Count > 0 ? errors : null;
        }

        if (!TargetTables.All.Contains(request.TargetTable, StringComparer.OrdinalIgnoreCase))
        {
            errors["targetTable"] = ["Invalid target table."];
        }

        if (!AggregateFunctions.All.Contains(request.AggregateFunction, StringComparer.OrdinalIgnoreCase))
        {
            errors["aggregateFunction"] = ["Invalid aggregate function."];
        }

        var table = ReportTableRegistry.GetTable(request.TargetTable);
        if (table is null)
        {
            errors["targetTable"] = ["Target table is not supported."];
            return errors;
        }

        if (!table.AllowedAggregates.Contains(request.AggregateFunction, StringComparer.OrdinalIgnoreCase))
        {
            errors["aggregateFunction"] = [$"'{request.AggregateFunction}' is not allowed for {request.TargetTable}."];
        }

        var groupBy = request.GroupByColumns ?? [];
        var maxGroupBy = ReportTypes.MaxGroupByColumns(request.ReportType);
        if (groupBy.Count > maxGroupBy)
        {
            errors["groupByColumns"] = [$"Report type '{request.ReportType}' allows at most {maxGroupBy} group-by column(s)."];
        }

        foreach (var column in groupBy)
        {
            var metadata = ReportTableRegistry.GetColumn(request.TargetTable, column);
            if (metadata is null || !metadata.IsGroupable)
            {
                errors["groupByColumns"] = [$"'{column}' is not a groupable column on {request.TargetTable}."];
                break;
            }
        }

        if (!request.AggregateFunction.Equals(AggregateFunctions.Count, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.AggregateField))
            {
                errors["aggregateField"] = ["Aggregate field is required for this function."];
            }
            else
            {
                var aggregateColumn = ReportTableRegistry.GetColumn(request.TargetTable, request.AggregateField);
                if (aggregateColumn is null || !aggregateColumn.IsAggregatable)
                {
                    errors["aggregateField"] = [$"'{request.AggregateField}' is not aggregatable on {request.TargetTable}."];
                }
            }
        }

        if (request.ComparisonEnabled &&
            !request.ReportType.Equals(ReportTypes.MetricCard, StringComparison.OrdinalIgnoreCase))
        {
            errors["comparisonEnabled"] = ["Comparison is only supported for metric cards."];
        }

        foreach (var filter in request.Filters ?? [])
        {
            var filterColumn = ReportTableRegistry.GetColumn(request.TargetTable, filter.Field);
            if (filterColumn is null)
            {
                errors["filters"] = [$"'{filter.Field}' is not a valid column on {request.TargetTable}."];
                break;
            }
        }

        return errors.Count > 0 ? errors : null;
    }

    private static void ValidateApiTable(SaveReportRequest request, Dictionary<string, string[]> errors)
    {
        if (!ApiResources.All.Contains(request.TargetTable, StringComparer.OrdinalIgnoreCase))
        {
            errors["targetTable"] = ["Invalid API resource."];
            return;
        }

        try
        {
            ApiTableConfigParser.Parse(request.TargetTable, request.ChartOptionsJson);
        }
        catch (Exception exception)
        {
            errors["chartOptionsJson"] = [exception.Message];
        }
    }

    public static IReadOnlyDictionary<string, string[]>? ValidateDashboard(SaveDashboardRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Dashboard name is required."];
        }

        return errors.Count > 0 ? errors : null;
    }

    public static IReadOnlyDictionary<string, string[]>? ValidateSection(SaveDashboardSectionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!LayoutDirections.All.Contains(request.LayoutDirection, StringComparer.OrdinalIgnoreCase))
        {
            errors["layoutDirection"] = ["Layout direction must be Row or Column."];
        }

        if (!ReportSizes.All.Contains(request.Size, StringComparer.OrdinalIgnoreCase))
        {
            errors["size"] = ["Invalid section size."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
