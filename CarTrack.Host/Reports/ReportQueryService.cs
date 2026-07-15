using CarTrack.Modules.CoreHr;
using CarTrack.Modules.Expense;
using CarTrack.Modules.Fleet;
using CarTrack.Modules.Leave;
using CarTrack.Modules.Reporting;
using CarTrack.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Reports;

public class ReportQueryService(
    ReportingDbContext dbContext,
    FleetDbContext fleetDbContext,
    UsersDbContext usersDbContext,
    CoreHrDbContext coreHrDbContext,
    LeaveDbContext leaveDbContext,
    ExpenseDbContext expenseDbContext,
    IApiTableReportExecutor apiTableReportExecutor) : IReportQueryService
{
    public async Task<ReportExecutionResultDto> ExecuteAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await dbContext.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken)
            ?? throw new KeyNotFoundException($"Report '{reportId}' was not found.");

        return await ExecuteAsync(report, cancellationToken);
    }

    public Task<ReportExecutionResultDto> ExecuteAsync(
        ReportDefinition report,
        CancellationToken cancellationToken = default)
    {
        if (report.ReportType.Equals(ReportTypes.ApiTable, StringComparison.OrdinalIgnoreCase))
        {
            return apiTableReportExecutor.ExecuteAsync(report, cancellationToken);
        }

        if (report.ReportType.Equals(ReportTypes.Map, StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteMapAsync(report, cancellationToken);
        }

        var groupBy = ReportMapper.DeserializeList(report.GroupByColumnsJson).ToArray();
        var filters = ReportMapper.DeserializeFilters(report.FiltersJson);

        return report.TargetTable.Equals(TargetTables.Vehicles, StringComparison.OrdinalIgnoreCase)
            ? ExecuteVehiclesAsync(report, groupBy, filters, cancellationToken)
            : report.TargetTable.Equals(TargetTables.Drivers, StringComparison.OrdinalIgnoreCase)
                ? ExecuteDriversAsync(report, groupBy, filters, cancellationToken)
                : report.TargetTable.Equals(TargetTables.LeaveRequests, StringComparison.OrdinalIgnoreCase)
                    ? ExecuteLeaveRequestsAsync(report, groupBy, filters, cancellationToken)
                    : report.TargetTable.Equals(TargetTables.LeaveBalances, StringComparison.OrdinalIgnoreCase)
                        ? ExecuteLeaveBalancesAsync(report, groupBy, filters, cancellationToken)
                        : report.TargetTable.Equals(TargetTables.ExpenseClaims, StringComparison.OrdinalIgnoreCase)
                            ? ExecuteExpenseClaimsAsync(report, groupBy, filters, cancellationToken)
                            : report.TargetTable.Equals(TargetTables.Departments, StringComparison.OrdinalIgnoreCase)
                                ? ExecuteDepartmentsAsync(report, groupBy, filters, cancellationToken)
                                : report.TargetTable.Equals(TargetTables.StaffProfiles, StringComparison.OrdinalIgnoreCase)
                                    ? ExecuteStaffProfilesAsync(report, groupBy, filters, cancellationToken)
                                    : throw new InvalidOperationException($"Target table '{report.TargetTable}' is not supported.");
    }

    public Task<ReportExecutionResultDto> ExecutePreviewAsync(
        SaveReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = new ReportDefinition
        {
            Id = Guid.Empty,
            Name = request.Name,
            Description = request.Description,
            ReportType = request.ReportType,
            Size = request.Size,
            TargetTable = request.TargetTable,
            AggregateFunction = request.AggregateFunction,
            AggregateField = request.AggregateField,
            GroupByColumnsJson = ReportMapper.SerializeList(request.GroupByColumns ?? []),
            FiltersJson = ReportMapper.SerializeFilters(request.Filters ?? []),
            ComparisonEnabled = request.ComparisonEnabled,
            ChartOptionsJson = request.ChartOptionsJson,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        return ExecuteAsync(report, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteVehiclesAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<Vehicle> query = fleetDbContext.Vehicles.AsNoTracking().Where(vehicle => !vehicle.IsDeleted);
        query = ApplyFilters(query, TargetTables.Vehicles, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteDriversAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<Driver> query = fleetDbContext.Drivers.AsNoTracking();
        query = ApplyFilters(query, TargetTables.Drivers, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteLeaveRequestsAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<LeaveRequest> query = leaveDbContext.LeaveRequests.AsNoTracking();
        query = ApplyFilters(query, TargetTables.LeaveRequests, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteLeaveBalancesAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<LeaveBalance> query = leaveDbContext.LeaveBalances.AsNoTracking();
        query = ApplyFilters(query, TargetTables.LeaveBalances, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteExpenseClaimsAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<ExpenseClaim> query = expenseDbContext.ExpenseClaims.AsNoTracking();
        query = ApplyFilters(query, TargetTables.ExpenseClaims, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteDepartmentsAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<Department> query = coreHrDbContext.Departments.AsNoTracking();
        query = ApplyFilters(query, TargetTables.Departments, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteStaffProfilesAsync(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IReadOnlyList<ReportFilterDto> filters,
        CancellationToken cancellationToken)
    {
        IQueryable<StaffProfile> query = usersDbContext.StaffProfiles.AsNoTracking();
        query = ApplyFilters(query, TargetTables.StaffProfiles, filters);
        return await ExecuteQueryAsync(report, groupBy, query, cancellationToken);
    }

    private async Task<ReportExecutionResultDto> ExecuteQueryAsync<TEntity>(
        ReportDefinition report,
        IReadOnlyList<string> groupBy,
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var aggregateLabel = BuildAggregateLabel(report.AggregateFunction, report.AggregateField);

        if (groupBy.Count == 0)
        {
            var value = await AggregateAsync(query, report.AggregateFunction, report.AggregateField, cancellationToken);
            var metric = report.ComparisonEnabled
                ? await BuildMetricComparisonAsync(query, report.AggregateFunction, report.AggregateField, cancellationToken)
                : new ReportMetricDto(Round(value), null, null);

            return new ReportExecutionResultDto(
                report.Id.ToString(),
                report.ReportType,
                report.Name,
                ["Value"],
                [[Round(value)]],
                metric);
        }

        if (groupBy.Count == 1)
        {
            var rows = await GroupOneAsync(query, groupBy[0], report.AggregateFunction, report.AggregateField, cancellationToken);
            return new ReportExecutionResultDto(
                report.Id.ToString(),
                report.ReportType,
                report.Name,
                [GetColumnLabel(report.TargetTable, groupBy[0]), aggregateLabel],
                rows,
                null);
        }

        if (groupBy.Count == 2)
        {
            var rows = await GroupTwoAsync(
                query,
                groupBy[0],
                groupBy[1],
                report.AggregateFunction,
                report.AggregateField,
                cancellationToken);

            return new ReportExecutionResultDto(
                report.Id.ToString(),
                report.ReportType,
                report.Name,
                [
                    GetColumnLabel(report.TargetTable, groupBy[0]),
                    GetColumnLabel(report.TargetTable, groupBy[1]),
                    aggregateLabel,
                ],
                rows,
                null);
        }

        var multiRows = await GroupThreeAsync(
            query,
            groupBy[0],
            groupBy[1],
            groupBy[2],
            report.AggregateFunction,
            report.AggregateField,
            cancellationToken);

        return new ReportExecutionResultDto(
            report.Id.ToString(),
            report.ReportType,
            report.Name,
            [
                GetColumnLabel(report.TargetTable, groupBy[0]),
                GetColumnLabel(report.TargetTable, groupBy[1]),
                GetColumnLabel(report.TargetTable, groupBy[2]),
                aggregateLabel,
            ],
            multiRows,
            null);
    }

    private static async Task<double> AggregateAsync<TEntity>(
        IQueryable<TEntity> query,
        string aggregateFunction,
        string? aggregateField,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (aggregateFunction.Equals(AggregateFunctions.Count, StringComparison.OrdinalIgnoreCase))
        {
            return await query.CountAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(aggregateField))
        {
            throw new InvalidOperationException("Aggregate field is required.");
        }

        return aggregateFunction.ToUpperInvariant() switch
        {
            "SUM" => await SumAsync(query, aggregateField, cancellationToken),
            "AVERAGE" => await AverageAsync(query, aggregateField, cancellationToken),
            "MIN" => await MinAsync(query, aggregateField, cancellationToken),
            "MAX" => await MaxAsync(query, aggregateField, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported aggregate function '{aggregateFunction}'."),
        };
    }

    private static async Task<double> SumAsync<TEntity>(
        IQueryable<TEntity> query,
        string field,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (IsDateColumn(field))
        {
            var value = await query.SumAsync(
                entity => (double?)EF.Property<DateOnly?>(entity, field)!.Value.DayNumber,
                cancellationToken);
            return value ?? 0;
        }

        var numeric = await query.SumAsync(
            entity => (double?)EF.Property<double?>(entity, field) ?? EF.Property<int?>(entity, field),
            cancellationToken);
        return numeric ?? 0;
    }

    private static async Task<double> AverageAsync<TEntity>(
        IQueryable<TEntity> query,
        string field,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (IsDateColumn(field))
        {
            var value = await query.AverageAsync(
                entity => (double?)EF.Property<DateOnly?>(entity, field)!.Value.DayNumber,
                cancellationToken);
            return value ?? 0;
        }

        var numeric = await query.AverageAsync(
            entity => (double?)EF.Property<double?>(entity, field) ?? EF.Property<int?>(entity, field),
            cancellationToken);
        return numeric ?? 0;
    }

    private static async Task<double> MinAsync<TEntity>(
        IQueryable<TEntity> query,
        string field,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (IsDateColumn(field))
        {
            var value = await query.MinAsync(
                entity => (double?)EF.Property<DateOnly?>(entity, field)!.Value.DayNumber,
                cancellationToken);
            return value ?? 0;
        }

        var numeric = await query.MinAsync(
            entity => (double?)EF.Property<double?>(entity, field) ?? EF.Property<int?>(entity, field),
            cancellationToken);
        return numeric ?? 0;
    }

    private static async Task<double> MaxAsync<TEntity>(
        IQueryable<TEntity> query,
        string field,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (IsDateColumn(field))
        {
            var value = await query.MaxAsync(
                entity => (double?)EF.Property<DateOnly?>(entity, field)!.Value.DayNumber,
                cancellationToken);
            return value ?? 0;
        }

        var numeric = await query.MaxAsync(
            entity => (double?)EF.Property<double?>(entity, field) ?? EF.Property<int?>(entity, field),
            cancellationToken);
        return numeric ?? 0;
    }

    private static async Task<ReportMetricDto> BuildMetricComparisonAsync<TEntity>(
        IQueryable<TEntity> query,
        string aggregateFunction,
        string? aggregateField,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var now = DateTimeOffset.UtcNow;
        var startOfThisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);

        var currentQuery = query.Where(entity => EF.Property<DateTimeOffset>(entity, "CreatedAt") >= startOfThisMonth);
        var previousQuery = query.Where(entity =>
            EF.Property<DateTimeOffset>(entity, "CreatedAt") >= startOfLastMonth &&
            EF.Property<DateTimeOffset>(entity, "CreatedAt") < startOfThisMonth);

        var currentValue = await AggregateAsync(currentQuery, aggregateFunction, aggregateField, cancellationToken);
        var previousValue = await AggregateAsync(previousQuery, aggregateFunction, aggregateField, cancellationToken);
        double? changePercent = previousValue == 0
            ? null
            : Round(((currentValue - previousValue) / previousValue) * 100);

        return new ReportMetricDto(Round(currentValue), Round(previousValue), changePercent);
    }

    private static async Task<IReadOnlyList<object?[]>> GroupOneAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn,
        string aggregateFunction,
        string? aggregateField,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (IsDateTimeColumn(groupColumn))
        {
            return aggregateFunction.ToUpperInvariant() switch
            {
                "COUNT" => await GroupDateTimeCountAsync(query, groupColumn, cancellationToken),
                "SUM" => await GroupDateTimeNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Sum, cancellationToken),
                "AVERAGE" => await GroupDateTimeNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Average, cancellationToken),
                "MIN" => await GroupDateTimeNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Min, cancellationToken),
                "MAX" => await GroupDateTimeNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Max, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported aggregate function '{aggregateFunction}'."),
            };
        }

        if (IsDateColumn(groupColumn))
        {
            return aggregateFunction.ToUpperInvariant() switch
            {
                "COUNT" => await GroupDateOnlyCountAsync(query, groupColumn, cancellationToken),
                _ => throw new InvalidOperationException($"Aggregate '{aggregateFunction}' is not supported for date grouping."),
            };
        }

        return aggregateFunction.ToUpperInvariant() switch
        {
            "COUNT" => await GroupStringCountAsync(query, groupColumn, cancellationToken),
            "SUM" => await GroupStringNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Sum, cancellationToken),
            "AVERAGE" => await GroupStringNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Average, cancellationToken),
            "MIN" => await GroupStringNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Min, cancellationToken),
            "MAX" => await GroupStringNumericAsync(query, groupColumn, aggregateField!, AggregateFunctions.Max, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported aggregate function '{aggregateFunction}'."),
        };
    }

    private static async Task<IReadOnlyList<object?[]>> GroupDateTimeCountAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var grouped = await query
            .GroupBy(entity => new
            {
                Year = EF.Property<DateTimeOffset>(entity, groupColumn).Year,
                Month = EF.Property<DateTimeOffset>(entity, groupColumn).Month,
            })
            .Select(group => new GroupedValueRow(
                group.Key.Year,
                group.Key.Month,
                (double)group.Count()))
            .OrderBy(row => row.Year)
            .ThenBy(row => row.Month)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(row => new object?[] { $"{row.Year:D4}-{row.Month:D2}", Round(row.Value) })
            .ToList();
    }

    private static async Task<IReadOnlyList<object?[]>> GroupDateTimeNumericAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn,
        string aggregateField,
        string aggregateFunction,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var grouped = await query
            .GroupBy(entity => new
            {
                Year = EF.Property<DateTimeOffset>(entity, groupColumn).Year,
                Month = EF.Property<DateTimeOffset>(entity, groupColumn).Month,
            })
            .Select(group => new GroupedValueRow(
                group.Key.Year,
                group.Key.Month,
                aggregateFunction == AggregateFunctions.Sum
                    ? group.Sum(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                    : aggregateFunction == AggregateFunctions.Average
                        ? group.Average(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                        : aggregateFunction == AggregateFunctions.Min
                            ? group.Min(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                            : group.Max(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)))
            .OrderBy(row => row.Year)
            .ThenBy(row => row.Month)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(row => new object?[] { $"{row.Year:D4}-{row.Month:D2}", Round(row.Value) })
            .ToList();
    }

    private static async Task<IReadOnlyList<object?[]>> GroupDateOnlyCountAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var grouped = await query
            .GroupBy(entity => EF.Property<DateOnly?>(entity, groupColumn))
            .Select(group => new
            {
                Label = group.Key,
                Value = (double)group.Count(),
            })
            .OrderBy(row => row.Label)
            .ToListAsync(cancellationToken);

        return grouped
            .Select(row => new object?[] { FormatDateOnly(row.Label), Round(row.Value) })
            .ToList();
    }

    private static async Task<IReadOnlyList<object?[]>> GroupStringCountAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await query
            .GroupBy(entity => EF.Property<string>(entity, groupColumn) ?? string.Empty)
            .Select(group => new
            {
                Label = group.Key,
                Value = (double)group.Count(),
            })
            .OrderByDescending(row => row.Value)
            .ThenBy(row => row.Label)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new object?[]
            {
                string.IsNullOrWhiteSpace(row.Label) ? "Unknown" : row.Label,
                Round(row.Value),
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<object?[]>> GroupStringNumericAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn,
        string aggregateField,
        string aggregateFunction,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await query
            .GroupBy(entity => EF.Property<string>(entity, groupColumn) ?? string.Empty)
            .Select(group => new
            {
                Label = group.Key,
                Value = aggregateFunction == AggregateFunctions.Sum
                    ? group.Sum(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                    : aggregateFunction == AggregateFunctions.Average
                        ? group.Average(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                        : aggregateFunction == AggregateFunctions.Min
                            ? group.Min(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                            : group.Max(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0),
            })
            .OrderByDescending(row => row.Value)
            .ThenBy(row => row.Label)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new object?[]
            {
                string.IsNullOrWhiteSpace(row.Label) ? "Unknown" : row.Label,
                Round(row.Value),
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<object?[]>> GroupTwoAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn1,
        string groupColumn2,
        string aggregateFunction,
        string? aggregateField,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (aggregateFunction.Equals(AggregateFunctions.Count, StringComparison.OrdinalIgnoreCase))
        {
            var rows = await query
                .GroupBy(entity => new
                {
                    Key1 = EF.Property<string>(entity, groupColumn1) ?? string.Empty,
                    Key2 = EF.Property<string>(entity, groupColumn2) ?? string.Empty,
                })
                .Select(group => new
                {
                    group.Key.Key1,
                    group.Key.Key2,
                    Value = (double)group.Count(),
                })
                .OrderBy(row => row.Key1)
                .ThenBy(row => row.Key2)
                .ToListAsync(cancellationToken);

            return FormatTwoColumnRows(rows.Select(row => (row.Key1, row.Key2, row.Value)));
        }

        if (string.IsNullOrWhiteSpace(aggregateField))
        {
            throw new InvalidOperationException("Aggregate field is required.");
        }

        var numericRows = await query
            .GroupBy(entity => new
            {
                Key1 = EF.Property<string>(entity, groupColumn1) ?? string.Empty,
                Key2 = EF.Property<string>(entity, groupColumn2) ?? string.Empty,
            })
            .Select(group => new
            {
                group.Key.Key1,
                group.Key.Key2,
                Value = aggregateFunction == AggregateFunctions.Sum
                    ? group.Sum(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                    : aggregateFunction == AggregateFunctions.Average
                        ? group.Average(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                        : aggregateFunction == AggregateFunctions.Min
                            ? group.Min(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                            : group.Max(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0),
            })
            .OrderBy(row => row.Key1)
            .ThenBy(row => row.Key2)
            .ToListAsync(cancellationToken);

        return FormatTwoColumnRows(numericRows.Select(row => (row.Key1, row.Key2, row.Value)));
    }

    private static List<object?[]> FormatTwoColumnRows(
        IEnumerable<(string Key1, string Key2, double Value)> rows) =>
        rows
            .Select(row => new object?[]
            {
                string.IsNullOrWhiteSpace(row.Key1) ? "Unknown" : row.Key1,
                string.IsNullOrWhiteSpace(row.Key2) ? "Unknown" : row.Key2,
                Round(row.Value),
            })
            .ToList();

    private static List<object?[]> FormatThreeColumnRows(
        IEnumerable<(string Key1, string Key2, string Key3, double Value)> rows) =>
        rows
            .Select(row => new object?[]
            {
                string.IsNullOrWhiteSpace(row.Key1) ? "Unknown" : row.Key1,
                string.IsNullOrWhiteSpace(row.Key2) ? "Unknown" : row.Key2,
                string.IsNullOrWhiteSpace(row.Key3) ? "Unknown" : row.Key3,
                Round(row.Value),
            })
            .ToList();

    private static async Task<IReadOnlyList<object?[]>> GroupThreeAsync<TEntity>(
        IQueryable<TEntity> query,
        string groupColumn1,
        string groupColumn2,
        string groupColumn3,
        string aggregateFunction,
        string? aggregateField,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (aggregateFunction.Equals(AggregateFunctions.Count, StringComparison.OrdinalIgnoreCase))
        {
            var rows = await query
                .GroupBy(entity => new
                {
                    Key1 = EF.Property<string>(entity, groupColumn1) ?? string.Empty,
                    Key2 = EF.Property<string>(entity, groupColumn2) ?? string.Empty,
                    Key3 = EF.Property<string>(entity, groupColumn3) ?? string.Empty,
                })
                .Select(group => new
                {
                    group.Key.Key1,
                    group.Key.Key2,
                    group.Key.Key3,
                    Value = (double)group.Count(),
                })
                .OrderBy(row => row.Key1)
                .ThenBy(row => row.Key2)
                .ThenBy(row => row.Key3)
                .ToListAsync(cancellationToken);

            return FormatThreeColumnRows(rows.Select(row => (row.Key1, row.Key2, row.Key3, row.Value)));
        }

        if (string.IsNullOrWhiteSpace(aggregateField))
        {
            throw new InvalidOperationException("Aggregate field is required.");
        }

        var numericRows = await query
            .GroupBy(entity => new
            {
                Key1 = EF.Property<string>(entity, groupColumn1) ?? string.Empty,
                Key2 = EF.Property<string>(entity, groupColumn2) ?? string.Empty,
                Key3 = EF.Property<string>(entity, groupColumn3) ?? string.Empty,
            })
            .Select(group => new
            {
                group.Key.Key1,
                group.Key.Key2,
                group.Key.Key3,
                Value = aggregateFunction == AggregateFunctions.Sum
                    ? group.Sum(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                    : aggregateFunction == AggregateFunctions.Average
                        ? group.Average(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                        : aggregateFunction == AggregateFunctions.Min
                            ? group.Min(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0)
                            : group.Max(entity => (double?)EF.Property<double?>(entity, aggregateField) ?? EF.Property<int?>(entity, aggregateField) ?? 0),
            })
            .OrderBy(row => row.Key1)
            .ThenBy(row => row.Key2)
            .ThenBy(row => row.Key3)
            .ToListAsync(cancellationToken);

        return FormatThreeColumnRows(numericRows.Select(row => (row.Key1, row.Key2, row.Key3, row.Value)));
    }

    private static IQueryable<TEntity> ApplyFilters<TEntity>(
        IQueryable<TEntity> query,
        string targetTable,
        IReadOnlyList<ReportFilterDto> filters)
        where TEntity : class
    {
        foreach (var filter in filters)
        {
            var column = ReportTableRegistry.GetColumn(targetTable, filter.Field)
                ?? throw new InvalidOperationException($"Filter column '{filter.Field}' is not supported.");

            query = filter.Operator.ToLowerInvariant() switch
            {
                "equals" => ApplyEqualsFilter(query, filter.Field, filter.Value, column.Kind),
                "notequals" or "not_equals" => ApplyNotEqualsFilter(query, filter.Field, filter.Value, column.Kind),
                "contains" => query.Where(entity =>
                    (EF.Property<string>(entity, filter.Field) ?? string.Empty).Contains(filter.Value)),
                _ => throw new InvalidOperationException($"Filter operator '{filter.Operator}' is not supported."),
            };
        }

        return query;
    }

    private static IQueryable<TEntity> ApplyEqualsFilter<TEntity>(
        IQueryable<TEntity> query,
        string field,
        string value,
        ReportColumnKind kind)
        where TEntity : class =>
        kind switch
        {
            ReportColumnKind.Number when int.TryParse(value, out var intValue) =>
                query.Where(entity => EF.Property<int?>(entity, field) == intValue ||
                                      EF.Property<double?>(entity, field) == intValue),
            ReportColumnKind.Boolean when bool.TryParse(value, out var boolValue) =>
                query.Where(entity => EF.Property<bool?>(entity, field) == boolValue),
            _ => query.Where(entity => EF.Property<string>(entity, field) == value),
        };

    private static IQueryable<TEntity> ApplyNotEqualsFilter<TEntity>(
        IQueryable<TEntity> query,
        string field,
        string value,
        ReportColumnKind kind)
        where TEntity : class =>
        kind switch
        {
            ReportColumnKind.Number when int.TryParse(value, out var intValue) =>
                query.Where(entity => EF.Property<int?>(entity, field) != intValue &&
                                      EF.Property<double?>(entity, field) != intValue),
            ReportColumnKind.Boolean when bool.TryParse(value, out var boolValue) =>
                query.Where(entity => EF.Property<bool?>(entity, field) != boolValue),
            _ => query.Where(entity => EF.Property<string>(entity, field) != value),
        };

    private static string FormatDateOnly(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd") ?? "Unknown";

    private static bool IsDateTimeColumn(string column) =>
        column.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase);

    private static bool IsDateColumn(string column) =>
        column.Equals("LicenceExpiry", StringComparison.OrdinalIgnoreCase) ||
        column.Equals("WorkStartDate", StringComparison.OrdinalIgnoreCase) ||
        column.Equals("StartDate", StringComparison.OrdinalIgnoreCase) ||
        column.Equals("EndDate", StringComparison.OrdinalIgnoreCase) ||
        column.Equals("ExpenseDate", StringComparison.OrdinalIgnoreCase) ||
        column.Equals("CycleStart", StringComparison.OrdinalIgnoreCase) ||
        column.Equals("CycleEnd", StringComparison.OrdinalIgnoreCase);

    private static string GetColumnLabel(string targetTable, string column) =>
        ReportTableRegistry.GetColumn(targetTable, column)?.Label ?? column;

    private static string BuildAggregateLabel(string aggregateFunction, string? aggregateField) =>
        aggregateFunction.Equals(AggregateFunctions.Count, StringComparison.OrdinalIgnoreCase)
            ? "Count"
            : $"{aggregateFunction} ({aggregateField})";

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private async Task<ReportExecutionResultDto> ExecuteMapAsync(
        ReportDefinition report,
        CancellationToken cancellationToken)
    {
        var vehicles = await fleetDbContext.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted && v.Latitude != null && v.Longitude != null)
            .OrderBy(v => v.RegistrationNumber)
            .Select(v => new
            {
                v.RegistrationNumber,
                v.Latitude,
                v.Longitude,
                v.Make,
                v.Model,
                v.IgnitionStatus,
                v.PositionDescription,
                v.Speed,
            })
            .ToListAsync(cancellationToken);

        string[] columns = ["registration", "lat", "lng", "make", "model", "ignitionStatus", "positionDescription", "speed"];

        var rows = vehicles
            .Select(v => new object?[]
            {
                v.RegistrationNumber,
                v.Latitude,
                v.Longitude,
                string.IsNullOrEmpty(v.Make) ? null : (object)v.Make,
                string.IsNullOrEmpty(v.Model) ? null : (object)v.Model,
                v.IgnitionStatus,
                v.PositionDescription,
                v.Speed,
            })
            .ToList();

        return new ReportExecutionResultDto(
            report.Id.ToString(),
            report.ReportType,
            report.Name,
            columns,
            rows,
            null);
    }

    private sealed record GroupedValueRow(int Year, int Month, double Value);
}
