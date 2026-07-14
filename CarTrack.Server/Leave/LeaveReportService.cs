using System.Globalization;
using System.Text;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Leave;

public record LeaveReportCountDto(string Label, int Count);

public record LeaveReportSummaryDto(
    int PendingCount,
    int OnLeaveTodayCount,
    decimal TotalRemainingDays,
    decimal RemainingAnnualDays,
    decimal RemainingSickDays,
    decimal RemainingOtherDays,
    IReadOnlyList<LeaveReportCountDto> ByType,
    IReadOnlyList<LeaveReportCountDto> ByStatus,
    IReadOnlyList<LeaveReportCountDto> ByDepartment);

public record LeaveHistoryRowDto(
    Guid Id,
    string RequesterUserId,
    string ManagerUserId,
    string RequesterDisplayName,
    string? Department,
    string? Branch,
    string LeaveType,
    string StartDate,
    string EndDate,
    decimal WorkingDays,
    string Status,
    string? Notes,
    string CreatedAt,
    string? CreatedByUserId,
    string? CreatedByDisplayName,
    string UpdatedAt,
    string? UpdatedByUserId,
    string? UpdatedByDisplayName,
    string? DecidedAt);

public record LeaveHistoryFilters(
    string? Status,
    string? Department,
    string? Branch,
    string? StartDate,
    string? EndDate,
    string? UserId,
    int? Year);

public record LeaveLiabilityRowDto(
    string UserId,
    string DisplayName,
    string? Department,
    string LeaveTypeName,
    string LeaveTypeCode,
    string? LeaveTypeColor,
    decimal Allocated,
    decimal Used,
    decimal Pending,
    decimal Remaining);

public interface ILeaveReportService
{
    Task<LeaveReportSummaryDto> GetSummaryAsync(
        string viewerUserId,
        int? year,
        string? department,
        string? branch,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveHistoryRowDto>> GetHistoryAsync(
        string viewerUserId,
        LeaveHistoryFilters filters,
        CancellationToken cancellationToken = default);

    string BuildHistoryCsv(IReadOnlyList<LeaveHistoryRowDto> rows);

    Task<IReadOnlyList<LeaveLiabilityRowDto>> GetLiabilityAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetPendingReportAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default);
}

public class LeaveReportService(
    ApplicationDbContext dbContext,
    ICurrentUserScope currentUserScope) : ILeaveReportService
{
    public async Task<LeaveReportSummaryDto> GetSummaryAsync(
        string viewerUserId,
        int? year,
        string? department,
        string? branch,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await GetViewerProfileAsync(viewerUserId, cancellationToken);
        var visibleRows = await LoadVisibleHistoryRowsAsync(
            viewerUserId,
            scope,
            viewerProfile,
            new LeaveHistoryFilters(null, department, branch, null, null, null, targetYear),
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pendingCount = visibleRows.Count(row => row.Status == ApprovalStatuses.Pending);
        var onLeaveTodayCount = visibleRows.Count(row =>
            row.Status == ApprovalStatuses.Approved
            && DateOnly.TryParse(row.StartDate, out var start)
            && DateOnly.TryParse(row.EndDate, out var end)
            && start <= today
            && end >= today);

        var liability = await GetLiabilityAsync(viewerUserId, cancellationToken);
        var remainingAnnual = liability
            .Where(row => LeaveRemainingCategoryHelper.FromLeaveTypeCode(row.LeaveTypeCode) == LeaveRemainingCategory.Annual)
            .Sum(row => row.Remaining);
        var remainingSick = liability
            .Where(row => LeaveRemainingCategoryHelper.FromLeaveTypeCode(row.LeaveTypeCode) == LeaveRemainingCategory.Sick)
            .Sum(row => row.Remaining);
        var remainingOther = liability
            .Where(row => LeaveRemainingCategoryHelper.FromLeaveTypeCode(row.LeaveTypeCode) == LeaveRemainingCategory.Other)
            .Sum(row => row.Remaining);
        var totalRemaining = remainingAnnual + remainingSick + remainingOther;

        return new LeaveReportSummaryDto(
            pendingCount,
            onLeaveTodayCount,
            totalRemaining,
            remainingAnnual,
            remainingSick,
            remainingOther,
            GroupCounts(visibleRows, row => row.LeaveType),
            GroupCounts(visibleRows, row => row.Status),
            GroupCounts(
                visibleRows.Where(row => !string.IsNullOrWhiteSpace(row.Department)),
                row => row.Department!));
    }

    public async Task<IReadOnlyList<LeaveHistoryRowDto>> GetHistoryAsync(
        string viewerUserId,
        LeaveHistoryFilters filters,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await GetViewerProfileAsync(viewerUserId, cancellationToken);
        var rows = await LoadVisibleHistoryRowsAsync(viewerUserId, scope, viewerProfile, filters, cancellationToken);
        return rows
            .OrderByDescending(row => row.CreatedAt)
            .ToList();
    }

    public string BuildHistoryCsv(IReadOnlyList<LeaveHistoryRowDto> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Employee,Department,Branch,Leave Type,Start Date,End Date,Working Days,Status,Notes,Created At,Decided At");

        foreach (var row in rows)
        {
            builder.Append(CsvEscape(row.RequesterDisplayName)).Append(',');
            builder.Append(CsvEscape(row.Department)).Append(',');
            builder.Append(CsvEscape(row.Branch)).Append(',');
            builder.Append(CsvEscape(row.LeaveType)).Append(',');
            builder.Append(CsvEscape(row.StartDate)).Append(',');
            builder.Append(CsvEscape(row.EndDate)).Append(',');
            builder.Append(row.WorkingDays.ToString(CultureInfo.InvariantCulture)).Append(',');
            builder.Append(CsvEscape(row.Status)).Append(',');
            builder.Append(CsvEscape(row.Notes)).Append(',');
            builder.Append(CsvEscape(row.CreatedAt)).Append(',');
            builder.AppendLine(CsvEscape(row.DecidedAt));
        }

        return builder.ToString();
    }

    public async Task<IReadOnlyList<LeaveLiabilityRowDto>> GetLiabilityAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await GetViewerProfileAsync(viewerUserId, cancellationToken);
        var currentYear = DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var cycleStart = new DateOnly(currentYear, 1, 1);
        var cycleEnd = new DateOnly(currentYear, 12, 31);

        var balances = await dbContext.LeaveBalances
            .AsNoTracking()
            .Include(balance => balance.User)
            .Include(balance => balance.LeaveType)
            .Where(balance => balance.CycleStart == cycleStart && balance.CycleEnd == cycleEnd)
            .ToListAsync(cancellationToken);

        var userIds = balances.Select(balance => balance.UserId).Distinct().ToList();
        var profiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => userIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        return balances
            .Where(balance => LeaveVisibility.CanViewUser(
                scope,
                viewerUserId,
                viewerProfile,
                balance.UserId,
                profiles.GetValueOrDefault(balance.UserId)))
            .Select(balance =>
            {
                var profile = profiles.GetValueOrDefault(balance.UserId);
                return new LeaveLiabilityRowDto(
                    balance.UserId,
                    balance.User.DisplayName ?? balance.User.UserName ?? balance.User.Email ?? balance.UserId,
                    profile?.Department,
                    balance.LeaveType.Name,
                    balance.LeaveType.Code,
                    balance.LeaveType.Color,
                    balance.Allocated,
                    balance.Used,
                    balance.Pending,
                    balance.Allocated + balance.Adjusted - balance.Used - balance.Pending);
            })
            .OrderBy(row => row.DisplayName)
            .ThenBy(row => row.LeaveTypeName)
            .ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetPendingReportAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await GetViewerProfileAsync(viewerUserId, cancellationToken);

        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.Requester)
            .Include(request => request.LeaveType)
            .Include(request => request.CreatedByUser)
            .Include(request => request.UpdatedByUser)
            .Where(request => request.Status == ApprovalStatuses.Pending)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        var requesterIds = requests.Select(request => request.RequesterUserId).Distinct().ToList();
        var profiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => requesterIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        return requests
            .Where(request => LeaveVisibility.CanViewRequest(
                scope,
                viewerUserId,
                viewerProfile,
                request.RequesterUserId,
                profiles.GetValueOrDefault(request.RequesterUserId)))
            .Select(MapRequest)
            .ToList();
    }

    private async Task<List<LeaveHistoryRowDto>> LoadVisibleHistoryRowsAsync(
        string viewerUserId,
        UserDataScope scope,
        StaffProfile? viewerProfile,
        LeaveHistoryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.Requester)
            .Include(request => request.LeaveType)
            .Include(request => request.CreatedByUser)
            .Include(request => request.UpdatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "All")
        {
            query = query.Where(request => request.Status == filters.Status.Trim());
        }

        if (filters.Year is not null)
        {
            var yearStart = new DateOnly(filters.Year.Value, 1, 1);
            var yearEnd = new DateOnly(filters.Year.Value, 12, 31);
            query = query.Where(request => request.StartDate <= yearEnd && request.EndDate >= yearStart);
        }

        if (!string.IsNullOrWhiteSpace(filters.StartDate)
            && DateOnly.TryParse(filters.StartDate, out var startDate))
        {
            query = query.Where(request => request.EndDate >= startDate);
        }

        if (!string.IsNullOrWhiteSpace(filters.EndDate)
            && DateOnly.TryParse(filters.EndDate, out var endDate))
        {
            query = query.Where(request => request.StartDate <= endDate);
        }

        if (!string.IsNullOrWhiteSpace(filters.UserId))
        {
            query = query.Where(request => request.RequesterUserId == filters.UserId.Trim());
        }

        var requests = await query.ToListAsync(cancellationToken);
        var requesterIds = requests.Select(request => request.RequesterUserId).Distinct().ToList();
        var profiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => requesterIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        return requests
            .Where(request => LeaveVisibility.CanViewRequest(
                scope,
                viewerUserId,
                viewerProfile,
                request.RequesterUserId,
                profiles.GetValueOrDefault(request.RequesterUserId)))
            .Select(request =>
            {
                var profile = profiles.GetValueOrDefault(request.RequesterUserId);
                return new LeaveHistoryRowDto(
                    request.Id,
                    request.RequesterUserId,
                    request.ManagerUserId,
                    request.Requester.DisplayName ?? request.Requester.UserName ?? request.Requester.Email ?? request.RequesterUserId,
                    profile?.Department,
                    profile?.Branch ?? request.RequesterBranch,
                    request.LeaveType.Name,
                    request.StartDate.ToString("yyyy-MM-dd"),
                    request.EndDate.ToString("yyyy-MM-dd"),
                    request.WorkingDays,
                    request.Status,
                    request.Notes,
                    AuditableMapping.FormatTimestamp(request.CreatedAt),
                    request.CreatedByUserId,
                    AuditableMapping.UserDisplayName(request.CreatedByUser),
                    AuditableMapping.FormatTimestamp(request.UpdatedAt == default ? request.CreatedAt : request.UpdatedAt),
                    request.UpdatedByUserId,
                    AuditableMapping.UserDisplayName(request.UpdatedByUser),
                    AuditableMapping.FormatTimestamp(request.DecidedAt));
            })
            .Where(row => MatchesDepartment(row, filters.Department))
            .Where(row => MatchesBranch(row, filters.Branch))
            .ToList();
    }

    private async Task<StaffProfile?> GetViewerProfileAsync(
        string viewerUserId,
        CancellationToken cancellationToken) =>
        await dbContext.StaffProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.UserId == viewerUserId, cancellationToken);

    private static IReadOnlyList<LeaveReportCountDto> GroupCounts<T>(
        IEnumerable<T> rows,
        Func<T, string> selector) =>
        rows.GroupBy(selector, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LeaveReportCountDto(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Label)
            .ToList();

    private static bool MatchesDepartment(LeaveHistoryRowDto row, string? department) =>
        string.IsNullOrWhiteSpace(department)
        || string.Equals(row.Department, department.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool MatchesBranch(LeaveHistoryRowDto row, string? branch) =>
        string.IsNullOrWhiteSpace(branch)
        || string.Equals(row.Branch, branch.Trim(), StringComparison.OrdinalIgnoreCase);

    private static LeaveRequestDto MapRequest(LeaveRequest request) =>
        new(
            request.Id,
            request.RequesterUserId,
            request.Requester.DisplayName ?? request.Requester.UserName ?? request.Requester.Email ?? request.RequesterUserId,
            request.ManagerUserId,
            request.LeaveTypeId,
            request.LeaveType.Name,
            request.LeaveType.Color,
            request.StartDate.ToString("yyyy-MM-dd"),
            request.EndDate.ToString("yyyy-MM-dd"),
            request.StartDayPortion,
            request.EndDayPortion,
            request.WorkingDays,
            request.Status,
            request.Notes,
            !string.IsNullOrWhiteSpace(request.DocumentPath),
            request.DocumentFileName,
            AuditableMapping.FormatTimestamp(request.CreatedAt),
            request.CreatedByUserId,
            AuditableMapping.UserDisplayName(request.CreatedByUser),
            AuditableMapping.FormatTimestamp(request.UpdatedAt == default ? request.CreatedAt : request.UpdatedAt),
            request.UpdatedByUserId,
            AuditableMapping.UserDisplayName(request.UpdatedByUser),
            AuditableMapping.FormatTimestamp(request.DecidedAt));

    private static string CsvEscape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return text;
    }
}
