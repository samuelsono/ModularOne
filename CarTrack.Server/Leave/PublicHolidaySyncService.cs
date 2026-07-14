using CarTrack.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Server.Leave;

public interface IPublicHolidaySyncService
{
    Task SyncMissingHolidaysAsync(
        DateOnly validFrom,
        DateOnly validTo,
        CancellationToken cancellationToken = default);
}

public class PublicHolidaySyncService(
    ApplicationDbContext dbContext,
    IOpenHolidaysApiClient openHolidaysApiClient) : IPublicHolidaySyncService
{
    public async Task SyncMissingHolidaysAsync(
        DateOnly validFrom,
        DateOnly validTo,
        CancellationToken cancellationToken = default)
    {
        if (validTo < validFrom)
        {
            return;
        }

        var yearsNeedingSync = await GetYearsNeedingSyncAsync(validFrom, validTo, cancellationToken);
        if (yearsNeedingSync.Count == 0)
        {
            return;
        }

        var fetchFrom = new DateOnly(yearsNeedingSync.Min(), 1, 1);
        var fetchTo = new DateOnly(yearsNeedingSync.Max(), 12, 31);

        IReadOnlyList<OpenHolidayEntry> remoteHolidays;
        try
        {
            remoteHolidays = await openHolidaysApiClient.GetPublicHolidaysAsync(
                fetchFrom,
                fetchTo,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (remoteHolidays.Count == 0)
        {
            return;
        }

        var existingExternalIds = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday => holiday.ExternalId != null)
            .Select(holiday => holiday.ExternalId!)
            .ToListAsync(cancellationToken);

        var existingExternalIdSet = existingExternalIds.ToHashSet(StringComparer.Ordinal);

        var existingDateNamePairs = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday => holiday.Date >= fetchFrom && holiday.Date <= fetchTo)
            .Select(holiday => new { holiday.Date, holiday.Name })
            .ToListAsync(cancellationToken);

        var dateNameSet = existingDateNamePairs
            .Select(item => PublicHolidayResolver.CreateMatchKey(item.Date, item.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recurringHolidays = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday => holiday.IsRecurring)
            .ToListAsync(cancellationToken);

        for (var year = fetchFrom.Year; year <= fetchTo.Year; year++)
        {
            foreach (var projected in PublicHolidayResolver.ResolveForYear(recurringHolidays, year))
            {
                if (DateOnly.TryParse(projected.Date, out var projectedDate)
                    && projectedDate >= fetchFrom
                    && projectedDate <= fetchTo)
                {
                    dateNameSet.Add(PublicHolidayResolver.CreateMatchKey(projectedDate, projected.Name));
                }
            }
        }

        var added = false;
        foreach (var remoteHoliday in remoteHolidays)
        {
            if (existingExternalIdSet.Contains(remoteHoliday.ExternalId))
            {
                continue;
            }

            var dateNameKey = PublicHolidayResolver.CreateMatchKey(remoteHoliday.Date, remoteHoliday.Name);
            if (dateNameSet.Contains(dateNameKey))
            {
                continue;
            }

            dbContext.PublicHolidays.Add(new PublicHoliday
            {
                Id = Guid.NewGuid(),
                Name = remoteHoliday.Name,
                Date = remoteHoliday.Date,
                IsRecurring = false,
                Branch = null,
                ExternalId = remoteHoliday.ExternalId,
            });

            existingExternalIdSet.Add(remoteHoliday.ExternalId);
            dateNameSet.Add(dateNameKey);
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<int>> GetYearsNeedingSyncAsync(
        DateOnly validFrom,
        DateOnly validTo,
        CancellationToken cancellationToken)
    {
        var years = new List<int>();

        for (var year = validFrom.Year; year <= validTo.Year; year++)
        {
            var yearStart = new DateOnly(year, 1, 1);
            var yearEnd = new DateOnly(year, 12, 31);

            var hasApiCache = await dbContext.PublicHolidays
                .AnyAsync(
                    holiday => holiday.ExternalId != null
                        && holiday.Date >= yearStart
                        && holiday.Date <= yearEnd,
                    cancellationToken);

            if (!hasApiCache)
            {
                years.Add(year);
            }
        }

        return years;
    }
}
