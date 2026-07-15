
namespace CarTrack.Modules.Leave;

internal static class PublicHolidayResolver
{
    public static IReadOnlyList<PublicHolidayDto> ResolveForYear(
        IReadOnlyList<PublicHoliday> holidays,
        int targetYear)
    {
        var candidates = new List<(PublicHolidayDto Dto, int Priority)>();

        foreach (var holiday in holidays)
        {
            if (holiday.IsRecurring)
            {
                candidates.Add((MapForYear(holiday, targetYear), Priority: 1));
                continue;
            }

            if (holiday.Date.Year != targetYear)
            {
                continue;
            }

            candidates.Add((
                Map(holiday),
                Priority: holiday.ExternalId is not null ? 3 : 2));
        }

        return Deduplicate(candidates);
    }

    public static IReadOnlyList<PublicHolidayDto> ResolveForRange(
        IReadOnlyList<PublicHoliday> holidays,
        DateOnly startDate,
        DateOnly endDate)
    {
        var candidates = new List<(PublicHolidayDto Dto, int Priority)>();

        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            foreach (var (dto, priority) in BuildYearCandidates(holidays, year))
            {
                if (!DateOnly.TryParse(dto.Date, out var date)
                    || date < startDate
                    || date > endDate)
                {
                    continue;
                }

                candidates.Add((dto, priority));
            }
        }

        return Deduplicate(candidates);
    }

    public static HashSet<DateOnly> ResolveDatesForRange(
        IReadOnlyList<PublicHoliday> holidays,
        DateOnly startDate,
        DateOnly endDate,
        string? branch)
    {
        var dates = new HashSet<DateOnly>();

        foreach (var holiday in ResolveForRange(holidays, startDate, endDate))
        {
            if (!DateOnly.TryParse(holiday.Date, out var date))
            {
                continue;
            }

            if (AppliesToBranch(holiday.Branch, branch))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    private static IEnumerable<(PublicHolidayDto Dto, int Priority)> BuildYearCandidates(
        IReadOnlyList<PublicHoliday> holidays,
        int targetYear)
    {
        foreach (var holiday in holidays)
        {
            if (holiday.IsRecurring)
            {
                yield return (MapForYear(holiday, targetYear), 1);
                continue;
            }

            if (holiday.Date.Year != targetYear)
            {
                continue;
            }

            yield return (Map(holiday), holiday.ExternalId is not null ? 3 : 2);
        }
    }

    private static IReadOnlyList<PublicHolidayDto> Deduplicate(
        IReadOnlyList<(PublicHolidayDto Dto, int Priority)> candidates)
    {
        var byKey = new Dictionary<string, (PublicHolidayDto Dto, int Priority)>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var key = BuildKey(candidate.Dto.Date, candidate.Dto.Name);
            if (!byKey.TryGetValue(key, out var existing) || candidate.Priority > existing.Priority)
            {
                byKey[key] = candidate;
            }
        }

        return byKey.Values
            .Select(item => item.Dto)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private static PublicHolidayDto Map(PublicHoliday entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Date.ToString("yyyy-MM-dd"),
            entity.IsRecurring,
            entity.Branch);

    private static PublicHolidayDto MapForYear(PublicHoliday entity, int year) =>
        new(
            entity.Id,
            entity.Name,
            ToYearDate(year, entity.Date).ToString("yyyy-MM-dd"),
            entity.IsRecurring,
            entity.Branch);

    private static DateOnly ToYearDate(int year, DateOnly referenceDate)
    {
        var day = Math.Min(referenceDate.Day, DateTime.DaysInMonth(year, referenceDate.Month));
        return new DateOnly(year, referenceDate.Month, day);
    }

    private static string BuildKey(string date, string name) =>
        $"{date}|{NormalizeHolidayName(name)}";

    internal static string CreateMatchKey(DateOnly date, string name) =>
        BuildKey(date.ToString("yyyy-MM-dd"), name);

    private static string NormalizeHolidayName(string name) =>
        name.Trim()
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'')
            .ToLowerInvariant();

    private static bool AppliesToBranch(string? holidayBranch, string? filterBranch)
    {
        // National holidays (no branch) always apply.
        if (holidayBranch is null)
        {
            return true;
        }

        // Branch-specific holidays only apply when the requester's branch is known.
        if (filterBranch is null)
        {
            return false;
        }

        return string.Equals(holidayBranch, filterBranch, StringComparison.OrdinalIgnoreCase);
    }
}
