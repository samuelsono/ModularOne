using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Leave;

public record WorkingDaysResult(
    string StartDate,
    string EndDate,
    decimal WorkingDays,
    IReadOnlyList<string> HolidayDates,
    string StartDayPortion,
    string EndDayPortion);

public interface ILeaveWorkingDaysService
{
    Task<WorkingDaysResult> CalculateAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? branch,
        string? startDayPortion = null,
        string? endDayPortion = null,
        CancellationToken cancellationToken = default);
}

public class LeaveWorkingDaysService(
    ApplicationDbContext dbContext,
    IPublicHolidaySyncService publicHolidaySyncService) : ILeaveWorkingDaysService
{
    public async Task<WorkingDaysResult> CalculateAsync(
        DateOnly startDate,
        DateOnly endDate,
        string? branch,
        string? startDayPortion = null,
        string? endDayPortion = null,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var normalizedStartPortion = LeaveDayPortions.Normalize(startDayPortion);
        var normalizedEndPortion = LeaveDayPortions.Normalize(endDayPortion);

        await publicHolidaySyncService.SyncMissingHolidaysAsync(startDate, endDate, cancellationToken);

        var holidays = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday =>
                holiday.IsRecurring
                || (holiday.Date >= startDate && holiday.Date <= endDate))
            .ToListAsync(cancellationToken);

        var holidayDates = PublicHolidayResolver.ResolveDatesForRange(holidays, startDate, endDate, branch);
        var workingDays = CalculateWorkingDays(
            startDate,
            endDate,
            holidayDates,
            normalizedStartPortion,
            normalizedEndPortion);

        return new WorkingDaysResult(
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd"),
            workingDays,
            holidayDates.OrderBy(item => item).Select(item => item.ToString("yyyy-MM-dd")).ToList(),
            normalizedStartPortion,
            normalizedEndPortion);
    }

    internal static decimal CalculateWorkingDays(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlySet<DateOnly> holidayDates,
        string startDayPortion,
        string endDayPortion)
    {
        if (startDate == endDate)
        {
            if (!IsWorkingDay(startDate, holidayDates))
            {
                return 0m;
            }

            if (string.Equals(startDayPortion, LeaveDayPortions.Half, StringComparison.Ordinal)
                || string.Equals(endDayPortion, LeaveDayPortions.Half, StringComparison.Ordinal))
            {
                return 0.5m;
            }

            return 1m;
        }

        decimal workingDays = 0m;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (IsWorkingDay(date, holidayDates))
            {
                workingDays++;
            }
        }

        if (workingDays <= 0m)
        {
            return 0m;
        }

        if (string.Equals(startDayPortion, LeaveDayPortions.Half, StringComparison.Ordinal)
            && IsWorkingDay(startDate, holidayDates))
        {
            workingDays -= 0.5m;
        }

        if (string.Equals(endDayPortion, LeaveDayPortions.Half, StringComparison.Ordinal)
            && IsWorkingDay(endDate, holidayDates))
        {
            workingDays -= 0.5m;
        }

        return Math.Max(0m, workingDays);
    }

    private static bool IsWorkingDay(DateOnly date, IReadOnlySet<DateOnly> holidayDates) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
        && !holidayDates.Contains(date);
}
