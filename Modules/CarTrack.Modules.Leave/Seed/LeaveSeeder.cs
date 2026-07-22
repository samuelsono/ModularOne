using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Leave;

public static class LeaveSeeder
{
    public static readonly Guid AnnualTypeId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid SickTypeId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid FamilyTypeId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid UnpaidTypeId = Guid.Parse("11111111-1111-1111-1111-111111111104");

    private static readonly (Guid Id, string Name, string Code, string Color, bool IsPaid, bool DeductsBalance, bool RequiresDocument, bool AllowHalfDay, string AccrualMethod, decimal? AnnualEntitlement, int? MaxConsecutiveDays, int MinNoticeDays, int SortOrder)[] DefaultTypes =
    [
        (AnnualTypeId, "Annual", "ANNUAL", "#0078D4", true, true, false, true, LeaveAccrualMethods.Monthly, 15m, null, 7, 1),
        (SickTypeId, "Sick", "SICK", "#D13438", true, true, true, true, LeaveAccrualMethods.Upfront, 10m, null, 0, 2),
        (FamilyTypeId, "Family", "FAMILY", "#8764B8", true, true, false, false, LeaveAccrualMethods.Upfront, 3m, 3, 14, 3),
        (UnpaidTypeId, "Unpaid", "UNPAID", "#605E5C", false, false, false, true, LeaveAccrualMethods.Upfront, null, null, 7, 4),
    ];

    public static async Task SeedAsync(LeaveDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.LeaveTypes.AnyAsync(cancellationToken))
        {
            foreach (var type in DefaultTypes)
            {
                dbContext.LeaveTypes.Add(new LeaveType
                {
                    Id = type.Id,
                    Name = type.Name,
                    Code = type.Code,
                    Color = type.Color,
                    IsPaid = type.IsPaid,
                    DeductsBalance = type.DeductsBalance,
                    RequiresDocument = type.RequiresDocument,
                    AllowHalfDay = type.AllowHalfDay,
                    AccrualMethod = type.AccrualMethod,
                    AnnualEntitlement = type.AnnualEntitlement,
                    MaxConsecutiveDays = type.MaxConsecutiveDays,
                    MinNoticeDays = type.MinNoticeDays,
                    IsActive = true,
                    SortOrder = type.SortOrder,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await BackfillDefaultTypeValuesAsync(dbContext, cancellationToken);
        await SeedWorkLocationTypesAsync(dbContext, cancellationToken);
    }

    public static readonly Guid OfficeLocationId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid WfhLocationId = Guid.Parse("22222222-2222-2222-2222-222222222202");
    public static readonly Guid ClientLocationId = Guid.Parse("22222222-2222-2222-2222-222222222203");
    public static readonly Guid TravelLocationId = Guid.Parse("22222222-2222-2222-2222-222222222204");
    public static readonly Guid OtherLocationId = Guid.Parse("22222222-2222-2222-2222-222222222205");
    public static readonly Guid AbsentLocationId = Guid.Parse("22222222-2222-2222-2222-222222222206");

    private static readonly (Guid Id, string Code, string Name, string Color, bool TracksCollaborators, int SortOrder)[] DefaultLocations =
    [
        (OfficeLocationId, WorkLocationCodes.Office, "Office", "#0078D4", false, 1),
        (WfhLocationId, WorkLocationCodes.WorkFromHome, "Work from home", "#107C10", true, 2),
        (ClientLocationId, WorkLocationCodes.ClientSite, "Client site", "#8764B8", false, 3),
        (TravelLocationId, WorkLocationCodes.BusinessTravel, "Business travel", "#D83B01", false, 4),
        (OtherLocationId, WorkLocationCodes.Other, "Other", "#605E5C", false, 5),
        (AbsentLocationId, WorkLocationCodes.Absent, "Absent", "#A4262C", false, 6),
    ];

    private static async Task SeedWorkLocationTypesAsync(
        LeaveDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.WorkLocationTypes
            .Select(type => type.Code)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var location in DefaultLocations)
        {
            if (existing.Contains(location.Code))
            {
                continue;
            }

            dbContext.WorkLocationTypes.Add(new WorkLocationType
            {
                Id = location.Id,
                Code = location.Code,
                Name = location.Name,
                Color = location.Color,
                TracksCollaborators = location.TracksCollaborators,
                IsActive = true,
                SortOrder = location.SortOrder,
            });
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task BackfillWorkingDaysAsync(
        LeaveDbContext dbContext,
        ILeaveWorkingDaysService leaveWorkingDaysService,
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.LeaveRequests
            .Where(request => request.WorkingDays == 0m)
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            return;
        }

        var changed = false;

        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.StartDayPortion))
            {
                request.StartDayPortion = LeaveDayPortions.Full;
            }

            if (string.IsNullOrWhiteSpace(request.EndDayPortion))
            {
                request.EndDayPortion = LeaveDayPortions.Full;
            }

            var result = await leaveWorkingDaysService.CalculateAsync(
                request.StartDate,
                request.EndDate,
                request.RequesterBranch,
                request.StartDayPortion,
                request.EndDayPortion,
                cancellationToken);

            if (result.WorkingDays <= 0m)
            {
                continue;
            }

            request.WorkingDays = result.WorkingDays;
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task BackfillDefaultTypeValuesAsync(
        LeaveDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var changed = false;

        foreach (var defaults in DefaultTypes)
        {
            var entity = await dbContext.LeaveTypes
                .FirstOrDefaultAsync(type => type.Id == defaults.Id, cancellationToken);

            if (entity is null)
            {
                continue;
            }

            if (entity.AnnualEntitlement is null && defaults.AnnualEntitlement is not null)
            {
                entity.AnnualEntitlement = defaults.AnnualEntitlement;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(entity.AccrualMethod))
            {
                entity.AccrualMethod = defaults.AccrualMethod;
                changed = true;
            }
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
