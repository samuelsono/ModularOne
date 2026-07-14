using System.Text;
using System.Text.Json;
using CarTrack.Server.CarTrack;
using CarTrack.Server.Data;

namespace CarTrack.Server.Reports;

public interface IApiTableReportExecutor
{
    Task<ReportExecutionResultDto> ExecuteAsync(
        ReportDefinition report,
        CancellationToken cancellationToken = default);
}

public class ApiTableReportExecutor(ICarTrackApiClient carTrackApiClient) : IApiTableReportExecutor
{
    public async Task<ReportExecutionResultDto> ExecuteAsync(
        ReportDefinition report,
        CancellationToken cancellationToken = default)
    {
        var endpoint = report.TargetTable;
        if (!ApiResources.All.Contains(endpoint, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"API resource '{endpoint}' is not supported.");
        }

        var config = ApiTableConfigParser.Parse(endpoint, report.ChartOptionsJson);
        var dataArray = await FetchDataArrayAsync(config, cancellationToken);
        var columns = config.ColumnMappings.Select(mapping => mapping.Label).ToList();
        var rows = dataArray
            .EnumerateArray()
            .Take(ApiTableConfigParser.PerPage)
            .Select(item => config.ColumnMappings
                .Select(mapping => ApiTableValueExtractor.Extract(item, mapping.Field))
                .Cast<object?>()
                .ToArray())
            .ToList();

        return new ReportExecutionResultDto(
            report.Id.ToString(),
            report.ReportType,
            report.Name,
            columns,
            rows,
            null);
    }

    private Task<JsonElement> FetchDataArrayAsync(ApiTableConfig config, CancellationToken cancellationToken)
    {
        var path = BuildPath(config);
        return carTrackApiClient.GetDataArrayAsync(path, cancellationToken);
    }

    private static string BuildPath(ApiTableConfig config)
    {
        return config.Endpoint.ToLowerInvariant() switch
        {
            ApiResources.Vehicles =>
                BuildPagedPath("rest/vehicles"),
            ApiResources.VehicleStatus =>
                "rest/vehicles/status",
            ApiResources.Events =>
                BuildRangePath(
                    $"rest/vehicles/{Uri.EscapeDataString(config.Registration!)}/events",
                    config.StartTimestamp!.Value,
                    config.EndTimestamp!.Value),
            ApiResources.Trips =>
                BuildRangePath(
                    $"rest/trips/{Uri.EscapeDataString(config.Registration!)}",
                    config.StartTimestamp!.Value,
                    config.EndTimestamp!.Value),
            ApiResources.Alerts =>
                BuildPagedPath("rest/alerts"),
            ApiResources.AlertNotifications =>
                BuildDateFilteredPagedPath(
                    "rest/alerts/notifications",
                    config.StartTimestamp!.Value,
                    config.EndTimestamp!.Value),
            _ => throw new InvalidOperationException($"API resource '{config.Endpoint}' is not supported."),
        };
    }

    private static string BuildPagedPath(string basePath) =>
        $"{basePath}?page=1&per_page={ApiTableConfigParser.PerPage}";

    private static string BuildDateFilteredPagedPath(
        string basePath,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp)
    {
        const string format = "yyyy-MM-dd HH:mm:ss";
        return new StringBuilder(basePath)
            .Append("?filter[date_from]=")
            .Append(Uri.EscapeDataString(startTimestamp.ToString(format)))
            .Append("&filter[date_to]=")
            .Append(Uri.EscapeDataString(endTimestamp.ToString(format)))
            .Append("&page=1&per_page=")
            .Append(ApiTableConfigParser.PerPage)
            .ToString();
    }

    private static string BuildRangePath(
        string basePath,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp)
    {
        const string format = "yyyy-MM-dd HH:mm:ss";
        return new StringBuilder(basePath)
            .Append("?start_timestamp=")
            .Append(Uri.EscapeDataString(startTimestamp.ToString(format)))
            .Append("&end_timestamp=")
            .Append(Uri.EscapeDataString(endTimestamp.ToString(format)))
            .Append("&page=1&per_page=")
            .Append(ApiTableConfigParser.PerPage)
            .ToString();
    }
}
