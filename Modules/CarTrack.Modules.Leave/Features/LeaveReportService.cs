using System.Globalization;
using System.Text;
using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Leave;

public class LeaveReportService(
    LeaveDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager) : ILeaveReportService
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
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(viewerUserId, cancellationToken);
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
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(viewerUserId, cancellationToken);
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
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(viewerUserId, cancellationToken);
        var currentYear = DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var cycleStart = new DateOnly(currentYear, 1, 1);
        var cycleEnd = new DateOnly(currentYear, 12, 31);

        var balances = await dbContext.LeaveBalances
            .AsNoTracking()
            .Include(balance => balance.LeaveType)
            .Where(balance => balance.CycleStart == cycleStart && balance.CycleEnd == cycleEnd)
            .ToListAsync(cancellationToken);

        var userIds = balances.Select(balance => balance.UserId).Distinct().ToList();
        var profiles = await orgDirectory.GetStaffOrgInfoBatchAsync(userIds, cancellationToken);
        var names = await ResolveDisplayNamesAsync(userIds, cancellationToken);

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
                    names.GetValueOrDefault(balance.UserId) ?? balance.UserId,
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
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(viewerUserId, cancellationToken);

        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.LeaveType)
            .Where(request => request.Status == ApprovalStatuses.Pending)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        var requesterIds = requests.Select(request => request.RequesterUserId).Distinct().ToList();
        var profiles = await orgDirectory.GetStaffOrgInfoBatchAsync(requesterIds, cancellationToken);
        var names = await ResolveDisplayNamesAsync(
            requests.SelectMany(r => new[] { r.RequesterUserId, r.CreatedByUserId, r.UpdatedByUserId }),
            cancellationToken);

        return requests
            .Where(request => LeaveVisibility.CanViewRequest(
                scope,
                viewerUserId,
                viewerProfile,
                request.RequesterUserId,
                profiles.GetValueOrDefault(request.RequesterUserId)))
            .Select(request => MapRequest(request, names))
            .ToList();
    }

    private async Task<List<LeaveHistoryRowDto>> LoadVisibleHistoryRowsAsync(
        string viewerUserId,
        UserDataScope scope,
        StaffOrgInfo? viewerProfile,
        LeaveHistoryFilters filters,
        CancellationToken cancellationToken)
    {
        var query = dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.LeaveType)
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
        var profiles = await orgDirectory.GetStaffOrgInfoBatchAsync(requesterIds, cancellationToken);
        var names = await ResolveDisplayNamesAsync(
            requests.SelectMany(r => new[] { r.RequesterUserId, r.CreatedByUserId, r.UpdatedByUserId }),
            cancellationToken);

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
                    names.GetValueOrDefault(request.RequesterUserId) ?? request.RequesterUserId,
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
                    NameOrNull(names, request.CreatedByUserId),
                    AuditableMapping.FormatTimestamp(request.UpdatedAt == default ? request.CreatedAt : request.UpdatedAt),
                    request.UpdatedByUserId,
                    NameOrNull(names, request.UpdatedByUserId),
                    AuditableMapping.FormatTimestamp(request.DecidedAt));
            })
            .Where(row => MatchesDepartment(row, filters.Department))
            .Where(row => MatchesBranch(row, filters.Branch))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.DisplayName ?? user.UserName ?? user.Email ?? user.Id,
                StringComparer.Ordinal,
                cancellationToken);
    }

    private static string? NameOrNull(IReadOnlyDictionary<string, string> names, string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? null : names.GetValueOrDefault(userId);

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

    private static LeaveRequestDto MapRequest(LeaveRequest request, IReadOnlyDictionary<string, string> names) =>
        new(
            request.Id,
            request.RequesterUserId,
            names.GetValueOrDefault(request.RequesterUserId) ?? request.RequesterUserId,
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
            NameOrNull(names, request.CreatedByUserId),
            AuditableMapping.FormatTimestamp(request.UpdatedAt == default ? request.CreatedAt : request.UpdatedAt),
            request.UpdatedByUserId,
            NameOrNull(names, request.UpdatedByUserId),
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
