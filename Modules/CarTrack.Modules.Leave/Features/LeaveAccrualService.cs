namespace CarTrack.Modules.Leave;

public class LeaveAccrualService(
    LeaveDbContext dbContext,
    IOrgDirectory orgDirectory) : ILeaveAccrualService
{
    public async Task<int> RunMonthlyAccrualAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentYearMonth = ToYearMonth(today.Year, today.Month);
        var (cycleStart, cycleEnd) = GetCycleForDate(today);

        var accrualTypes = await dbContext.LeaveTypes
            .AsNoTracking()
            .Where(type => type.IsActive
                && type.DeductsBalance
                && type.AccrualMethod == LeaveAccrualMethods.Monthly)
            .ToListAsync(cancellationToken);

        if (accrualTypes.Count == 0)
        {
            return 0;
        }

        var staffProfiles = await orgDirectory.GetActiveStaffForAccrualAsync(cancellationToken);

        var accrualsApplied = 0;

        foreach (var profile in staffProfiles)
        {
            foreach (var leaveType in accrualTypes)
            {
                var entitlement = LeaveEntitlementHelper.ResolveAnnualEntitlement(leaveType);
                if (entitlement <= 0m)
                {
                    continue;
                }

                var monthlyRate = Math.Round(entitlement / 12m, 2, MidpointRounding.AwayFromZero);
                if (monthlyRate <= 0m)
                {
                    continue;
                }

                var balance = await dbContext.LeaveBalances
                    .FirstOrDefaultAsync(
                        item => item.UserId == profile.UserId
                            && item.LeaveTypeId == leaveType.Id
                            && item.CycleStart == cycleStart
                            && item.CycleEnd == cycleEnd,
                        cancellationToken);

                if (balance is null)
                {
                    balance = new LeaveBalance
                    {
                        Id = Guid.NewGuid(),
                        UserId = profile.UserId,
                        LeaveTypeId = leaveType.Id,
                        CycleStart = cycleStart,
                        CycleEnd = cycleEnd,
                        Allocated = 0m,
                        Used = 0m,
                        Pending = 0m,
                        Adjusted = 0m,
                    };
                    dbContext.LeaveBalances.Add(balance);
                }

                var accrualStartYearMonth = ResolveAccrualStartYearMonth(profile.WorkStartDate, cycleStart);
                var nextYearMonth = balance.LastAccruedYearMonth.HasValue
                    ? AddMonths(balance.LastAccruedYearMonth.Value, 1)
                    : accrualStartYearMonth;

                while (nextYearMonth <= currentYearMonth)
                {
                    if (IsEmployedDuringMonth(profile.WorkStartDate, nextYearMonth))
                    {
                        balance.Allocated += monthlyRate;
                        accrualsApplied++;
                    }

                    balance.LastAccruedYearMonth = nextYearMonth;
                    nextYearMonth = AddMonths(nextYearMonth, 1);
                }
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return accrualsApplied;
    }

    private static int ResolveAccrualStartYearMonth(DateOnly? workStartDate, DateOnly cycleStart)
    {
        if (workStartDate is null || workStartDate.Value <= cycleStart)
        {
            return ToYearMonth(cycleStart.Year, cycleStart.Month);
        }

        return ToYearMonth(workStartDate.Value.Year, workStartDate.Value.Month);
    }

    private static bool IsEmployedDuringMonth(DateOnly? workStartDate, int yearMonth)
    {
        if (workStartDate is null)
        {
            return true;
        }

        var monthStart = new DateOnly(yearMonth / 100, yearMonth % 100, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        return workStartDate.Value <= monthEnd;
    }

    private static (DateOnly CycleStart, DateOnly CycleEnd) GetCycleForDate(DateOnly date) =>
        (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31));

    private static int ToYearMonth(int year, int month) => year * 100 + month;

    private static int AddMonths(int yearMonth, int months)
    {
        var year = yearMonth / 100;
        var month = yearMonth % 100;
        var date = new DateOnly(year, month, 1).AddMonths(months);
        return ToYearMonth(date.Year, date.Month);
    }
}

internal static class LeaveEntitlementHelper
{
    private const decimal DefaultAnnualEntitlement = 15m;

    public static decimal ResolveAnnualEntitlement(LeaveType leaveType)
    {
        if (leaveType.AnnualEntitlement is decimal entitlement && entitlement > 0m)
        {
            return entitlement;
        }

        return string.Equals(leaveType.Code, "ANNUAL", StringComparison.OrdinalIgnoreCase)
            ? DefaultAnnualEntitlement
            : 0m;
    }

    public static decimal ResolveUpfrontAllocation(LeaveType leaveType)
    {
        if (!string.Equals(leaveType.AccrualMethod, LeaveAccrualMethods.Upfront, StringComparison.Ordinal))
        {
            return 0m;
        }

        return ResolveAnnualEntitlement(leaveType);
    }
}
