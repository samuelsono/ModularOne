using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Leave;

public record LeaveCalendarEntryDto(
    Guid RequestId,
    string UserId,
    string DisplayName,
    string? Department,
    string? Branch,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeColor,
    string StartDate,
    string EndDate,
    string Status,
    decimal WorkingDays);

public record LeaveCalendarResponse(
    IReadOnlyList<LeaveCalendarEntryDto> Entries,
    IReadOnlyList<PublicHolidayDto> Holidays,
    IReadOnlyList<string> Departments,
    IReadOnlyList<string> Branches);

public record LeaveCalendarFilters(
    string StartDate,
    string EndDate,
    string? Branch,
    string? Department,
    string? UserId);

public interface ILeaveCalendarService
{
    Task<LeaveCalendarResponse> GetCalendarAsync(
        string viewerUserId,
        LeaveCalendarFilters filters,
        CancellationToken cancellationToken = default);
}

public class LeaveCalendarService(
    ApplicationDbContext dbContext,
    ICurrentUserScope currentUserScope,
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
        var viewerProfile = await dbContext.StaffProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.UserId == viewerUserId, cancellationToken);

        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.Requester)
            .Include(request => request.LeaveType)
            .Where(request =>
                (request.Status == ApprovalStatuses.Approved || request.Status == ApprovalStatuses.Pending)
                && request.StartDate <= endDate
                && request.EndDate >= startDate)
            .ToListAsync(cancellationToken);

        var requesterIds = requests.Select(request => request.RequesterUserId).Distinct().ToList();
        var profiles = await dbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => requesterIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

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
                    request.Requester.DisplayName ?? request.Requester.UserName ?? request.Requester.Email ?? request.RequesterUserId,
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
