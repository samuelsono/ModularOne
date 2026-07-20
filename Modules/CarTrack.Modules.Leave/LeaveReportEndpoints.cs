using System.Security.Claims;
using System.Text;
using CarTrack.Server.Users.Authorization;

namespace CarTrack.Modules.Leave;

public static class LeaveReportEndpoints
{
    public static RouteGroupBuilder MapLeaveReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/reports/summary", GetSummaryAsync)
            .RequireAnyPermission("leave.reports.read", "leave.requests.read");

        group.MapGet("/reports/history", GetHistoryAsync)
            .RequireAnyPermission("leave.reports.read", "leave.requests.read");

        group.MapGet("/reports/liability", GetLiabilityAsync)
            .RequireAnyPermission("leave.reports.read", "leave.requests.read");

        group.MapGet("/reports/pending", GetPendingAsync)
            .RequireAnyPermission("leave.reports.read", "leave.requests.read");

        return group;
    }

    private static async Task<IResult> GetSummaryAsync(
        int? year,
        string? department,
        string? branch,
        ILeaveReportService leaveReportService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var summary = await leaveReportService.GetSummaryAsync(userId, year, department, branch, cancellationToken);
        return Results.Ok(summary);
    }

    private static async Task<IResult> GetHistoryAsync(
        string? status,
        string? department,
        string? branch,
        string? startDate,
        string? endDate,
        string? userId,
        int? year,
        string? format,
        ILeaveReportService leaveReportService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var viewerUserId = GetUserId(principal);
        if (viewerUserId is null)
        {
            return Results.Unauthorized();
        }

        var rows = await leaveReportService.GetHistoryAsync(
            viewerUserId,
            new LeaveHistoryFilters(status, department, branch, startDate, endDate, userId, year),
            cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = leaveReportService.BuildHistoryCsv(rows);
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", "leave-history.csv");
        }

        return Results.Ok(rows);
    }

    private static async Task<IResult> GetLiabilityAsync(
        ILeaveReportService leaveReportService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var rows = await leaveReportService.GetLiabilityAsync(userId, cancellationToken);
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetPendingAsync(
        ILeaveReportService leaveReportService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var rows = await leaveReportService.GetPendingReportAsync(userId, cancellationToken);
        return Results.Ok(rows);
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
