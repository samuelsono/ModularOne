using CarTrack.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Leave;

public class LeaveCalendarService(
    LeaveDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager,
    IPublicHolidaySyncService publicHolidaySyncService) : ILeaveCalendarService
{
    public async Task<LeaveCalendarResponse> GetCalendarAsync(
        string viewerUserId,
        LeaveCalendarFilters filters,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(filters.StartDate, out var startDate)
            || !DateOnly.TryParse(filters.EndDate, out var endDate))
        {
            throw new InvalidOperationException("Invalid calendar date range.");
        }

        if (endDate < startDate)
        {
            throw new InvalidOperationException("Calendar end date must be on or after the start date.");
        }

        await publicHolidaySyncService.SyncMissingHolidaysAsync(startDate, endDate, cancellationToken);

        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(viewerUserId, cancellationToken);

        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.LeaveType)
            .Where(request =>
                (request.Status == ApprovalStatuses.Approved || request.Status == ApprovalStatuses.Pending)
                && request.StartDate <= endDate
                && request.EndDate >= startDate)
            .ToListAsync(cancellationToken);

        var requesterIds = requests.Select(request => request.RequesterUserId).Distinct().ToList();
        var profiles = await orgDirectory.GetStaffOrgInfoBatchAsync(requesterIds, cancellationToken);
        var displayNames = await ResolveDisplayNamesAsync(requesterIds, cancellationToken);

        var visibleEntries = requests
            .Where(request => LeaveVisibility.CanViewRequest(
                scope,
                viewerUserId,
                viewerProfile,
                request.RequesterUserId,
                profiles.GetValueOrDefault(request.RequesterUserId)))
            .Select(request =>
            {
                var profile = profiles.GetValueOrDefault(request.RequesterUserId);
                return new LeaveCalendarEntryDto(
                    request.Id,
                    request.RequesterUserId,
                    displayNames.GetValueOrDefault(request.RequesterUserId) ?? request.RequesterUserId,
                    profile?.Department,
                    profile?.Branch ?? request.RequesterBranch,
                    request.LeaveTypeId,
                    request.LeaveType.Name,
                    request.LeaveType.Color,
                    request.StartDate.ToString("yyyy-MM-dd"),
                    request.EndDate.ToString("yyyy-MM-dd"),
                    request.Status,
                    request.WorkingDays);
            })
            .Where(entry => MatchesFilters(entry, filters))
            .OrderBy(entry => entry.StartDate)
            .ThenBy(entry => entry.DisplayName)
            .ToList();

        var holidays = await LoadHolidaysForRangeAsync(startDate, endDate, filters.Branch, cancellationToken);

        var departments = visibleEntries
            .Select(entry => entry.Department)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        var branches = visibleEntries
            .Select(entry => entry.Branch)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        return new LeaveCalendarResponse(visibleEntries, holidays, departments, branches);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();
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

    private static bool MatchesFilters(LeaveCalendarEntryDto entry, LeaveCalendarFilters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.UserId)
            && !string.Equals(entry.UserId, filters.UserId.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Branch)
            && !string.Equals(entry.Branch, filters.Branch.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Department)
            && !string.Equals(entry.Department, filters.Department.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private async Task<IReadOnlyList<PublicHolidayDto>> LoadHolidaysForRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? branch,
        CancellationToken cancellationToken)
    {
        var stored = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday =>
                holiday.IsRecurring
                || (holiday.Date >= startDate && holiday.Date <= endDate))
            .ToListAsync(cancellationToken);

        return PublicHolidayResolver.ResolveForRange(stored, startDate, endDate)
            .Where(holiday => holiday.Branch is null
                || branch is null
                || string.Equals(holiday.Branch, branch, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
