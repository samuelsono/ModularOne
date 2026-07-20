using CarTrack.Server.Data;
using CarTrack.Identity.Contracts;

namespace CarTrack.Modules.Leave;

public class LeaveConfigurationService(
    LeaveDbContext dbContext,
    IPublicHolidaySyncService publicHolidaySyncService,
    IOrgDirectory orgDirectory) : ILeaveConfigurationService
{
    public async Task<IReadOnlyList<LeaveTypeDto>> GetActiveTypesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var staffInfo = await orgDirectory.GetStaffOrgInfoAsync(userId, cancellationToken);
        var gender = staffInfo?.Gender;
        var types = await dbContext.LeaveTypes
            .AsNoTracking()
            .Where(type => type.IsActive
                && (type.EligibleGender == LeaveTypeGenderEligibility.Any
                    || type.EligibleGender == gender))
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .ToListAsync(cancellationToken);

        return types.Select(MapType).ToList();
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetAdminTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await dbContext.LeaveTypes
            .AsNoTracking()
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .ToListAsync(cancellationToken);

        return types.Select(MapType).ToList();
    }

    public async Task<LeaveTypeDto> CreateTypeAsync(
        SaveLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTypeRequest(request);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.LeaveTypes.AnyAsync(type => type.Code == code, cancellationToken))
        {
            throw new InvalidOperationException($"Leave type code '{code}' already exists.");
        }

        var entity = new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            Color = NormalizeColor(request.Color),
            IsPaid = request.IsPaid,
            DeductsBalance = request.DeductsBalance,
            RequiresDocument = request.RequiresDocument,
            AllowHalfDay = request.AllowHalfDay,
            AccrualMethod = LeaveAccrualMethods.Normalize(request.AccrualMethod),
            AnnualEntitlement = request.AnnualEntitlement,
            MaxConsecutiveDays = request.MaxConsecutiveDays,
            MinNoticeDays = request.MinNoticeDays,
            EligibleGender = LeaveTypeGenderEligibility.Normalize(request.EligibleGender),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.LeaveTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapType(entity);
    }

    public async Task<LeaveTypeDto?> UpdateTypeAsync(
        Guid id,
        SaveLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTypeRequest(request);

        var entity = await dbContext.LeaveTypes
            .FirstOrDefaultAsync(type => type.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.LeaveTypes.AnyAsync(
                type => type.Id != id && type.Code == code,
                cancellationToken))
        {
            throw new InvalidOperationException($"Leave type code '{code}' already exists.");
        }

        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.Color = NormalizeColor(request.Color);
        entity.IsPaid = request.IsPaid;
        entity.DeductsBalance = request.DeductsBalance;
        entity.RequiresDocument = request.RequiresDocument;
        entity.AllowHalfDay = request.AllowHalfDay;
        entity.AccrualMethod = LeaveAccrualMethods.Normalize(request.AccrualMethod);
        entity.AnnualEntitlement = request.AnnualEntitlement;
        entity.MaxConsecutiveDays = request.MaxConsecutiveDays;
        entity.MinNoticeDays = request.MinNoticeDays;
        entity.EligibleGender = LeaveTypeGenderEligibility.Normalize(request.EligibleGender);
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapType(entity);
    }

    public async Task<bool> DeleteTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.LeaveTypes
            .Include(type => type.Requests)
            .FirstOrDefaultAsync(type => type.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        if (entity.Requests.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete a leave type that has requests.");
        }

        dbContext.LeaveTypes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PublicHolidayDto>> GetAdminHolidaysAsync(
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;

        await publicHolidaySyncService.SyncMissingHolidaysAsync(
            new DateOnly(targetYear, 1, 1),
            new DateOnly(targetYear, 12, 31),
            cancellationToken);

        var holidays = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday => holiday.IsRecurring || holiday.Date.Year == targetYear)
            .ToListAsync(cancellationToken);

        return PublicHolidayResolver.ResolveForYear(holidays, targetYear);
    }

    public async Task<IReadOnlyList<PublicHolidayDto>> GetUpcomingHolidaysAsync(
        int untilYear,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (untilYear < today.Year)
        {
            return Array.Empty<PublicHolidayDto>();
        }

        var endDate = ResolveUpcomingHolidaysEndDate(today, untilYear);

        for (var year = today.Year; year <= endDate.Year; year++)
        {
            await publicHolidaySyncService.SyncMissingHolidaysAsync(
                new DateOnly(year, 1, 1),
                new DateOnly(year, 12, 31),
                cancellationToken);
        }

        var holidays = await dbContext.PublicHolidays
            .AsNoTracking()
            .Where(holiday => holiday.IsRecurring
                || (holiday.Date.Year >= today.Year && holiday.Date.Year <= endDate.Year))
            .ToListAsync(cancellationToken);

        return PublicHolidayResolver.ResolveForRange(holidays, today, endDate)
            .Where(holiday => DateOnly.Parse(holiday.Date) >= today)
            .ToList();
    }

    internal static DateOnly ResolveUpcomingHolidaysEndDate(DateOnly today, int untilYear)
    {
        var endOfUntilYear = new DateOnly(untilYear, 12, 31);
        if (untilYear > today.Year)
        {
            return endOfUntilYear;
        }

        var plusThreeMonths = today.AddMonths(3);
        return plusThreeMonths > endOfUntilYear ? plusThreeMonths : endOfUntilYear;
    }

    public async Task<PublicHolidayDto> CreateHolidayAsync(
        SavePublicHolidayRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateHolidayRequest(request);

        var entity = new PublicHoliday
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Date = DateOnly.Parse(request.Date),
            IsRecurring = request.IsRecurring,
            Branch = string.IsNullOrWhiteSpace(request.Branch) ? null : request.Branch.Trim(),
        };

        dbContext.PublicHolidays.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapHoliday(entity);
    }

    public async Task<PublicHolidayDto?> UpdateHolidayAsync(
        Guid id,
        SavePublicHolidayRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateHolidayRequest(request);

        var entity = await dbContext.PublicHolidays
            .FirstOrDefaultAsync(holiday => holiday.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name.Trim();
        entity.Date = DateOnly.Parse(request.Date);
        entity.IsRecurring = request.IsRecurring;
        entity.Branch = string.IsNullOrWhiteSpace(request.Branch) ? null : request.Branch.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapHoliday(entity);
    }

    public async Task<bool> DeleteHolidayAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PublicHolidays
            .FirstOrDefaultAsync(holiday => holiday.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.PublicHolidays.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SyncHolidaysAsync(
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var validFrom = new DateOnly(targetYear, 1, 1);
        var validTo = new DateOnly(targetYear, 12, 31);

        var beforeCount = await dbContext.PublicHolidays
            .CountAsync(
                holiday => holiday.Date >= validFrom && holiday.Date <= validTo,
                cancellationToken);

        await publicHolidaySyncService.SyncMissingHolidaysAsync(validFrom, validTo, cancellationToken);

        var afterCount = await dbContext.PublicHolidays
            .CountAsync(
                holiday => holiday.Date >= validFrom && holiday.Date <= validTo,
                cancellationToken);

        return afterCount - beforeCount;
    }

    private static LeaveTypeDto MapType(LeaveType entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Code,
            entity.Color,
            entity.IsPaid,
            entity.DeductsBalance,
            entity.RequiresDocument,
            entity.AllowHalfDay,
            entity.AccrualMethod,
            entity.AnnualEntitlement,
            entity.MaxConsecutiveDays,
            entity.MinNoticeDays,
            entity.EligibleGender,
            entity.IsActive,
            entity.SortOrder,
            AuditableMapping.FormatTimestamp(entity.CreatedAt),
            entity.CreatedByUserId,
            null,
            AuditableMapping.FormatTimestamp(entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt),
            entity.UpdatedByUserId,
            null);

    private static PublicHolidayDto MapHoliday(PublicHoliday entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Date.ToString("yyyy-MM-dd"),
            entity.IsRecurring,
            entity.Branch);

    private static void ValidateTypeRequest(SaveLeaveTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Leave type name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("Leave type code is required.");
        }

        if (request.MinNoticeDays < 0)
        {
            throw new InvalidOperationException("Minimum notice days cannot be negative.");
        }

        if (request.MaxConsecutiveDays is int maxConsecutiveDays && maxConsecutiveDays < 1)
        {
            throw new InvalidOperationException("Maximum consecutive days must be at least 1.");
        }

        if (!LeaveAccrualMethods.IsValid(request.AccrualMethod))
        {
            throw new InvalidOperationException("Accrual method must be Upfront or Monthly.");
        }

        _ = LeaveTypeGenderEligibility.Normalize(request.EligibleGender);

        if (request.AnnualEntitlement is < 0)
        {
            throw new InvalidOperationException("Annual entitlement cannot be negative.");
        }
    }

    private static void ValidateHolidayRequest(SavePublicHolidayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Holiday name is required.");
        }

        if (!DateOnly.TryParse(request.Date, out _))
        {
            throw new InvalidOperationException("Holiday date is invalid.");
        }
    }

    private static string NormalizeColor(string? color) =>
        string.IsNullOrWhiteSpace(color) ? "#0078D4" : color.Trim();
}
