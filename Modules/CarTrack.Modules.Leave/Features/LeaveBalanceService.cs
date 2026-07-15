using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Leave;

public class LeaveBalanceService(LeaveDbContext dbContext, IOrgDirectory orgDirectory) : ILeaveBalanceService, ILeaveBalanceStore
{
    public async Task<IReadOnlyList<LeaveBalanceDto>> GetMyBalancesAsync(
        string userId,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        await EnsureBalanceRowsAsync(userId, targetYear, cancellationToken);
        var balances = await LoadBalancesAsync(userId, targetYear, cancellationToken);
        return balances.Select(Map).ToList();
    }
    public async Task<IReadOnlyList<LeaveBalanceDto>> GetUserBalancesAsync(
        string userId,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        await EnsureBalanceRowsAsync(userId, targetYear, cancellationToken);
        var balances = await LoadBalancesAsync(userId, targetYear, cancellationToken);
        return balances.Select(Map).ToList();
    }
    public async Task<LeaveBalanceDto> AdjustBalanceAsync(
        AdjustLeaveBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(request.CycleStart, out var cycleStart)
            || !DateOnly.TryParse(request.CycleEnd, out var cycleEnd))
        {
            throw new InvalidOperationException("Invalid balance cycle dates.");
        }
        if (cycleEnd < cycleStart)
        {
            throw new InvalidOperationException("Cycle end must be on or after cycle start.");
        }
        var leaveTypeExists = await dbContext.LeaveTypes
            .AnyAsync(type => type.Id == request.LeaveTypeId, cancellationToken);
        if (!leaveTypeExists)
        {
            throw new InvalidOperationException("Leave type was not found.");
        }
        var userExists = await orgDirectory.UserExistsAsync(request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User was not found.");
        }
        var balance = await GetOrCreateBalanceAsync(
            request.UserId,
            request.LeaveTypeId,
            cycleStart,
            cycleEnd,
            cancellationToken);
        balance.Allocated += request.AllocatedDelta;
        balance.Adjusted += request.AdjustedDelta;
        if (balance.Allocated < 0 || balance.Adjusted < 0 || balance.Used < 0 || balance.Pending < 0)
        {
            throw new InvalidOperationException("Balance values cannot be negative.");
        }
        var remaining = balance.Allocated + balance.Adjusted - balance.Used - balance.Pending;
        if (remaining < 0)
        {
            throw new InvalidOperationException("Adjustment would leave a negative remaining balance.");
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(balance).Reference(item => item.LeaveType).LoadAsync(cancellationToken);
        return Map(balance);
    }
    public async Task<LeaveBalance> GetOrCreateBalanceAsync(
        string userId,
        Guid leaveTypeId,
        DateOnly cycleStart,
        DateOnly cycleEnd,
        CancellationToken cancellationToken = default)
    {
        var balance = await dbContext.LeaveBalances
            .Include(item => item.LeaveType)
            .FirstOrDefaultAsync(
                item => item.UserId == userId
                    && item.LeaveTypeId == leaveTypeId
                    && item.CycleStart == cycleStart
                    && item.CycleEnd == cycleEnd,
                cancellationToken);
        if (balance is not null)
        {
            return balance;
        }
        var leaveType = await dbContext.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == leaveTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Leave type was not found.");
        balance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            CycleStart = cycleStart,
            CycleEnd = cycleEnd,
            Allocated = LeaveEntitlementHelper.ResolveUpfrontAllocation(leaveType),
            Used = 0m,
            Pending = 0m,
            Adjusted = 0m,
        };
        dbContext.LeaveBalances.Add(balance);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(balance).Reference(item => item.LeaveType).LoadAsync(cancellationToken);
        return balance;
    }
    private async Task<List<LeaveBalance>> LoadBalancesAsync(
        string userId,
        int year,
        CancellationToken cancellationToken)
    {
        var (cycleStart, cycleEnd) = GetCycleForYear(year);
        return await dbContext.LeaveBalances
            .AsNoTracking()
            .Include(balance => balance.LeaveType)
            .Where(balance => balance.UserId == userId
                && balance.CycleStart == cycleStart
                && balance.CycleEnd == cycleEnd)
            .OrderBy(balance => balance.LeaveType.SortOrder)
            .ThenBy(balance => balance.LeaveType.Name)
            .ToListAsync(cancellationToken);
    }
    private async Task EnsureBalanceRowsAsync(
        string userId,
        int year,
        CancellationToken cancellationToken)
    {
        var (cycleStart, cycleEnd) = GetCycleForYear(year);
        var deductingTypes = await dbContext.LeaveTypes
            .AsNoTracking()
            .Where(type => type.IsActive && type.DeductsBalance)
            .ToListAsync(cancellationToken);
        var existingTypeIds = await dbContext.LeaveBalances
            .AsNoTracking()
            .Where(balance => balance.UserId == userId
                && balance.CycleStart == cycleStart
                && balance.CycleEnd == cycleEnd)
            .Select(balance => balance.LeaveTypeId)
            .ToListAsync(cancellationToken);
        var existingSet = existingTypeIds.ToHashSet();
        foreach (var leaveType in deductingTypes.Where(type => !existingSet.Contains(type.Id)))
        {
            dbContext.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveType.Id,
                CycleStart = cycleStart,
                CycleEnd = cycleEnd,
                Allocated = LeaveEntitlementHelper.ResolveUpfrontAllocation(leaveType),
                Used = 0m,
                Pending = 0m,
                Adjusted = 0m,
            });
        }
        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
    private static (DateOnly CycleStart, DateOnly CycleEnd) GetCycleForYear(int year) =>
        (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
    private static LeaveBalanceDto Map(LeaveBalance balance) =>
        new(
            balance.Id,
            balance.UserId,
            balance.LeaveTypeId,
            balance.LeaveType.Name,
            balance.LeaveType.Color,
            balance.CycleStart.ToString("yyyy-MM-dd"),
            balance.CycleEnd.ToString("yyyy-MM-dd"),
            balance.Allocated,
            balance.Used,
            balance.Pending,
            balance.Adjusted,
            balance.Allocated + balance.Adjusted - balance.Used - balance.Pending);
}
