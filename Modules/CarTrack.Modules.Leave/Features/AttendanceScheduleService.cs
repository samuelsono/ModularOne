using CarTrack.Identity.Contracts;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Leave;

public interface IAttendanceScheduleService
{
    Task<IReadOnlyList<WorkLocationTypeDto>> GetLocationTypesAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<WorkLocationTypeDto> CreateLocationTypeAsync(
        SaveWorkLocationTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkLocationTypeDto?> UpdateLocationTypeAsync(
        Guid id,
        SaveWorkLocationTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleTemplateDto>> GetTemplatesAsync(
        string actingUserId,
        string? targetUserId,
        CancellationToken cancellationToken = default);

    Task<ScheduleTemplateDto> UpsertTemplateAsync(
        string actingUserId,
        UpsertScheduleTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleDayOverrideDto>> GetOverridesAsync(
        string actingUserId,
        string? targetUserId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<ScheduleDayOverrideDto> UpsertOverrideAsync(
        string actingUserId,
        UpsertScheduleDayOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteOverrideAsync(
        string actingUserId,
        Guid overrideId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResolvedScheduleDayDto>> GetResolvedScheduleAsync(
        string actingUserId,
        DateOnly from,
        DateOnly to,
        string? targetUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceDayDto>> GetAttendanceAsync(
        string actingUserId,
        DateOnly from,
        DateOnly to,
        string? targetUserId,
        CancellationToken cancellationToken = default);

    Task<AttendanceDayDto> UpsertAttendanceAsync(
        string actingUserId,
        DateOnly date,
        UpsertAttendanceDayRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceCompareRowDto>> GetCompareAsync(
        string actingUserId,
        DateOnly from,
        DateOnly to,
        string? targetUserId,
        CancellationToken cancellationToken = default);

    Task<AttendancePolicySettingsDto> GetAttendancePolicyAsync(
        CancellationToken cancellationToken = default);

    Task<AttendancePolicySettingsDto> UpdateAttendancePolicyAsync(
        UpdateAttendancePolicySettingsRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class AttendanceScheduleService(
    LeaveDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager) : IAttendanceScheduleService
{
    public async Task<IReadOnlyList<WorkLocationTypeDto>> GetLocationTypesAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkLocationTypes.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(type => type.IsActive);
        }

        var items = await query
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .ToListAsync(cancellationToken);

        return items.Select(MapLocation).ToList();
    }

    public async Task<WorkLocationTypeDto> CreateLocationTypeAsync(
        SaveWorkLocationTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = new WorkLocationType
        {
            Id = Guid.NewGuid(),
            Code = NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            Color = NormalizeColor(request.Color),
            TracksCollaborators = request.TracksCollaborators,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        dbContext.WorkLocationTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapLocation(entity);
    }

    public async Task<WorkLocationTypeDto?> UpdateLocationTypeAsync(
        Guid id,
        SaveWorkLocationTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.WorkLocationTypes.SingleOrDefaultAsync(type => type.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Code = NormalizeCode(request.Code);
        entity.Name = request.Name.Trim();
        entity.Color = NormalizeColor(request.Color);
        entity.TracksCollaborators = request.TracksCollaborators;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapLocation(entity);
    }

    public async Task<IReadOnlyList<ScheduleTemplateDto>> GetTemplatesAsync(
        string actingUserId,
        string? targetUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var userId = await ResolveTargetUserIdAsync(scope, actingUserId, targetUserId, forWrite: false, cancellationToken);

        var templates = await dbContext.ScheduleTemplates
            .AsNoTracking()
            .Include(template => template.Days)
            .ThenInclude(day => day.LocationType)
            .Where(template => template.UserId == userId)
            .OrderByDescending(template => template.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var displayNames = await ResolveDisplayNamesAsync([userId], cancellationToken);
        return templates.Select(template => MapTemplate(template, displayNames)).ToList();
    }

    public async Task<ScheduleTemplateDto> UpsertTemplateAsync(
        string actingUserId,
        UpsertScheduleTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(request.EffectiveFrom, out var effectiveFrom))
        {
            throw new InvalidOperationException("Effective from date is invalid.");
        }

        DateOnly? effectiveTo = null;
        if (!string.IsNullOrWhiteSpace(request.EffectiveTo))
        {
            if (!DateOnly.TryParse(request.EffectiveTo, out var parsedTo))
            {
                throw new InvalidOperationException("Effective to date is invalid.");
            }

            if (parsedTo < effectiveFrom)
            {
                throw new InvalidOperationException("Effective to must be on or after effective from.");
            }

            effectiveTo = parsedTo;
        }

        if (request.Days is null || request.Days.Count == 0)
        {
            throw new InvalidOperationException("At least one schedule day is required.");
        }

        foreach (var day in request.Days)
        {
            if (day.DayOfWeek is < 0 or > 6)
            {
                throw new InvalidOperationException("Day of week must be between 0 (Sunday) and 6 (Saturday).");
            }
        }

        var locationIds = request.Days.Select(day => day.LocationTypeId).Distinct().ToList();
        var locations = await dbContext.WorkLocationTypes
            .Where(type => locationIds.Contains(type.Id) && type.IsActive)
            .ToDictionaryAsync(type => type.Id, cancellationToken);

        if (locations.Count != locationIds.Count)
        {
            throw new InvalidOperationException("One or more location types are invalid or inactive.");
        }

        if (locations.Values.Any(type =>
                type.Code.Equals(WorkLocationCodes.Absent, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Absent cannot be used as a planned schedule location.");
        }

        var scope = await currentUserScope.GetAsync(cancellationToken);
        var userId = await ResolveTargetUserIdAsync(scope, actingUserId, request.UserId, forWrite: true, cancellationToken);

        var existing = await dbContext.ScheduleTemplates
            .Include(template => template.Days)
            .Where(template => template.UserId == userId && template.EffectiveFrom == effectiveFrom)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            existing = new ScheduleTemplate
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EffectiveFrom = effectiveFrom,
            };
            dbContext.ScheduleTemplates.Add(existing);
        }
        else
        {
            dbContext.ScheduleTemplateDays.RemoveRange(existing.Days);
            existing.Days.Clear();
        }

        existing.EffectiveTo = effectiveTo;
        existing.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        foreach (var day in request.Days.GroupBy(item => item.DayOfWeek).Select(group => group.Last()))
        {
            existing.Days.Add(new ScheduleTemplateDay
            {
                Id = Guid.NewGuid(),
                TemplateId = existing.Id,
                DayOfWeek = day.DayOfWeek,
                LocationTypeId = day.LocationTypeId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.ScheduleTemplates
            .AsNoTracking()
            .Include(template => template.Days)
            .ThenInclude(day => day.LocationType)
            .SingleAsync(template => template.Id == existing.Id, cancellationToken);

        var displayNames = await ResolveDisplayNamesAsync([userId], cancellationToken);
        return MapTemplate(reloaded, displayNames);
    }

    public async Task<IReadOnlyList<ScheduleDayOverrideDto>> GetOverridesAsync(
        string actingUserId,
        string? targetUserId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var userId = await ResolveTargetUserIdAsync(scope, actingUserId, targetUserId, forWrite: false, cancellationToken);

        var query = dbContext.ScheduleDayOverrides
            .AsNoTracking()
            .Include(item => item.LocationType)
            .Where(item => item.UserId == userId);

        if (from is not null)
        {
            query = query.Where(item => item.Date >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(item => item.Date <= to.Value);
        }

        var items = await query.OrderBy(item => item.Date).ToListAsync(cancellationToken);
        return items.Select(MapOverride).ToList();
    }

    public async Task<ScheduleDayOverrideDto> UpsertOverrideAsync(
        string actingUserId,
        UpsertScheduleDayOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(request.Date, out var date))
        {
            throw new InvalidOperationException("Override date is invalid.");
        }

        var location = await dbContext.WorkLocationTypes
            .SingleOrDefaultAsync(type => type.Id == request.LocationTypeId && type.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Location type is invalid or inactive.");

        if (location.Code.Equals(WorkLocationCodes.Absent, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Absent cannot be used as a planned schedule location.");
        }

        var scope = await currentUserScope.GetAsync(cancellationToken);
        var userId = await ResolveTargetUserIdAsync(scope, actingUserId, request.UserId, forWrite: true, cancellationToken);

        var entity = await dbContext.ScheduleDayOverrides
            .Include(item => item.LocationType)
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Date == date, cancellationToken);

        if (entity is null)
        {
            entity = new ScheduleDayOverride
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
            };
            dbContext.ScheduleDayOverrides.Add(entity);
        }

        entity.LocationTypeId = location.Id;
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(entity).Reference(item => item.LocationType).LoadAsync(cancellationToken);
        return MapOverride(entity);
    }

    public async Task<bool> DeleteOverrideAsync(
        string actingUserId,
        Guid overrideId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ScheduleDayOverrides
            .SingleOrDefaultAsync(item => item.Id == overrideId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var scope = await currentUserScope.GetAsync(cancellationToken);
        if (!scope.CanManageUserAttendance(actingUserId, entity.UserId))
        {
            throw new UnauthorizedAccessException("You cannot delete this schedule override.");
        }

        dbContext.ScheduleDayOverrides.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ResolvedScheduleDayDto>> GetResolvedScheduleAsync(
        string actingUserId,
        DateOnly from,
        DateOnly to,
        string? targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(actingUserId, cancellationToken);
        var userIds = await ResolveVisibleUserIdsAsync(
            scope,
            actingUserId,
            viewerProfile,
            targetUserId,
            cancellationToken);

        if (userIds.Count == 0)
        {
            return [];
        }

        var templates = await dbContext.ScheduleTemplates
            .AsNoTracking()
            .Include(template => template.Days)
            .ThenInclude(day => day.LocationType)
            .Where(template => userIds.Contains(template.UserId))
            .ToListAsync(cancellationToken);

        var overrides = await dbContext.ScheduleDayOverrides
            .AsNoTracking()
            .Include(item => item.LocationType)
            .Where(item => userIds.Contains(item.UserId) && item.Date >= from && item.Date <= to)
            .ToListAsync(cancellationToken);

        var leaveRequests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.LeaveType)
            .Where(request =>
                userIds.Contains(request.RequesterUserId)
                && request.Status == ApprovalStatuses.Approved
                && request.StartDate <= to
                && request.EndDate >= from)
            .ToListAsync(cancellationToken);

        var displayNames = await ResolveDisplayNamesAsync(userIds, cancellationToken);
        var results = new List<ResolvedScheduleDayDto>();

        foreach (var userId in userIds)
        {
            var userTemplates = templates
                .Where(template => template.UserId == userId)
                .OrderByDescending(template => template.EffectiveFrom)
                .ToList();
            var userOverrides = overrides
                .Where(item => item.UserId == userId)
                .ToDictionary(item => item.Date);
            var userLeave = leaveRequests.Where(request => request.RequesterUserId == userId).ToList();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                results.Add(ResolveDay(
                    userId,
                    displayNames.GetValueOrDefault(userId) ?? userId,
                    date,
                    userTemplates,
                    userOverrides,
                    userLeave));
            }
        }

        return results
            .OrderBy(item => item.UserDisplayName)
            .ThenBy(item => item.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<AttendanceDayDto>> GetAttendanceAsync(
        string actingUserId,
        DateOnly from,
        DateOnly to,
        string? targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        var scope = await currentUserScope.GetAsync(cancellationToken);
        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(actingUserId, cancellationToken);
        var userIds = await ResolveVisibleUserIdsAsync(
            scope,
            actingUserId,
            viewerProfile,
            targetUserId,
            cancellationToken);

        var items = await dbContext.AttendanceDays
            .AsNoTracking()
            .Include(item => item.PlannedLocationType)
            .Include(item => item.ActualLocationType)
            .Include(item => item.Collaborators)
            .Where(item => userIds.Contains(item.UserId) && item.Date >= from && item.Date <= to)
            .OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);

        var nameIds = items
            .Select(item => item.UserId)
            .Concat(items.Select(item => item.RecordedByUserId))
            .Concat(items.SelectMany(item => item.Collaborators)
                .Select(collaborator => collaborator.CollaboratorUserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))!)
            .Distinct()
            .ToList();

        var displayNames = await ResolveDisplayNamesAsync(nameIds!, cancellationToken);
        return items.Select(item => MapAttendance(item, displayNames)).ToList();
    }

    public async Task<AttendanceDayDto> UpsertAttendanceAsync(
        string actingUserId,
        DateOnly date,
        UpsertAttendanceDayRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var userId = await ResolveTargetUserIdAsync(scope, actingUserId, request.UserId, forWrite: true, cancellationToken);

        var actualLocation = await dbContext.WorkLocationTypes
            .SingleOrDefaultAsync(type => type.Id == request.ActualLocationTypeId && type.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Actual location type is invalid or inactive.");

        var planned = await ResolveSingleDayAsync(userId, date, cancellationToken);
        Guid? plannedLocationId = planned.Kind == PlannedAttendanceKinds.Work
            ? planned.LocationTypeId
            : null;

        var entity = await dbContext.AttendanceDays
            .Include(item => item.Collaborators)
            .Include(item => item.PlannedLocationType)
            .Include(item => item.ActualLocationType)
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Date == date, cancellationToken);

        if (entity is null)
        {
            entity = new AttendanceDay
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                Source = ResolveAttendanceSource(scope, actingUserId, userId),
                RecordedByUserId = actingUserId,
            };
            dbContext.AttendanceDays.Add(entity);
        }
        else
        {
            dbContext.AttendanceCollaborators.RemoveRange(entity.Collaborators);
            entity.Collaborators.Clear();
            entity.Source = ResolveAttendanceSource(scope, actingUserId, userId);
            entity.RecordedByUserId = actingUserId;
        }

        entity.PlannedLocationTypeId = plannedLocationId;
        entity.ActualLocationTypeId = actualLocation.Id;
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.RecordedAt = DateTimeOffset.UtcNow;

        if (actualLocation.TracksCollaborators && request.Collaborators is { Count: > 0 })
        {
            foreach (var collaborator in request.Collaborators)
            {
                var hasUser = !string.IsNullOrWhiteSpace(collaborator.CollaboratorUserId);
                var hasExternal = !string.IsNullOrWhiteSpace(collaborator.ExternalName);
                if (!hasUser && !hasExternal)
                {
                    continue;
                }

                entity.Collaborators.Add(new AttendanceCollaborator
                {
                    Id = Guid.NewGuid(),
                    AttendanceDayId = entity.Id,
                    CollaboratorUserId = hasUser ? collaborator.CollaboratorUserId!.Trim() : null,
                    ExternalName = hasExternal ? collaborator.ExternalName!.Trim() : null,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await dbContext.AttendanceDays
            .AsNoTracking()
            .Include(item => item.PlannedLocationType)
            .Include(item => item.ActualLocationType)
            .Include(item => item.Collaborators)
            .SingleAsync(item => item.Id == entity.Id, cancellationToken);

        var nameIds = new List<string> { reloaded.UserId, reloaded.RecordedByUserId };
        nameIds.AddRange(reloaded.Collaborators
            .Select(collaborator => collaborator.CollaboratorUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))!);

        var displayNames = await ResolveDisplayNamesAsync(nameIds, cancellationToken);
        return MapAttendance(reloaded, displayNames);
    }

    public async Task<IReadOnlyList<AttendanceCompareRowDto>> GetCompareAsync(
        string actingUserId,
        DateOnly from,
        DateOnly to,
        string? targetUserId,
        CancellationToken cancellationToken = default)
    {
        var planned = await GetResolvedScheduleAsync(actingUserId, from, to, targetUserId, cancellationToken);
        var actual = await GetAttendanceAsync(actingUserId, from, to, targetUserId, cancellationToken);
        var actualByKey = actual.ToDictionary(item => $"{item.UserId}|{item.Date}", StringComparer.Ordinal);

        return planned.Select(day =>
        {
            var key = $"{day.UserId}|{day.Date}";
            actualByKey.TryGetValue(key, out var attendance);
            var hasActual = attendance is not null;
            var isMatch = hasActual
                && day.Kind == PlannedAttendanceKinds.Work
                && day.LocationTypeId is not null
                && attendance!.ActualLocationTypeId == day.LocationTypeId.Value;
            var isMismatch = hasActual
                && day.Kind == PlannedAttendanceKinds.Work
                && day.LocationTypeId is not null
                && attendance!.ActualLocationTypeId != day.LocationTypeId.Value;

            return new AttendanceCompareRowDto(
                day.UserId,
                day.UserDisplayName,
                day.Date,
                day.Kind,
                day.LocationTypeId,
                day.LocationTypeName,
                day.LocationTypeColor,
                attendance?.ActualLocationTypeId,
                attendance?.ActualLocationTypeName,
                attendance?.ActualLocationTypeColor,
                hasActual,
                isMatch,
                isMismatch,
                attendance?.Collaborators ?? []);
        }).ToList();
    }

    public async Task<AttendancePolicySettingsDto> GetAttendancePolicyAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAttendancePolicyAsync(cancellationToken);
        return MapAttendancePolicy(settings);
    }

    public async Task<AttendancePolicySettingsDto> UpdateAttendancePolicyAsync(
        UpdateAttendancePolicySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AttendanceDefaultAssumptions.IsValid(request.DefaultAssumption))
        {
            throw new InvalidOperationException("Default assumption must be Present or Absent.");
        }

        var settings = await GetOrCreateAttendancePolicyAsync(cancellationToken);
        settings.DefaultAssumption = AttendanceDefaultAssumptions.Normalize(request.DefaultAssumption);
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAttendancePolicy(settings);
    }

    private async Task<AttendancePolicySettings> GetOrCreateAttendancePolicyAsync(
        CancellationToken cancellationToken)
    {
        var settings = await dbContext.AttendancePolicySettings
            .SingleOrDefaultAsync(item => item.Id == AttendancePolicySettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new AttendancePolicySettings
        {
            Id = AttendancePolicySettings.SingletonId,
            DefaultAssumption = AttendanceDefaultAssumptions.Present,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.AttendancePolicySettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static AttendancePolicySettingsDto MapAttendancePolicy(AttendancePolicySettings settings) =>
        new(
            AttendanceDefaultAssumptions.Normalize(settings.DefaultAssumption),
            settings.UpdatedAt == default ? null : settings.UpdatedAt.ToString("O"));

    private async Task<ResolvedScheduleDayDto> ResolveSingleDayAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var templates = await dbContext.ScheduleTemplates
            .AsNoTracking()
            .Include(template => template.Days)
            .ThenInclude(day => day.LocationType)
            .Where(template => template.UserId == userId)
            .OrderByDescending(template => template.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var overrides = await dbContext.ScheduleDayOverrides
            .AsNoTracking()
            .Include(item => item.LocationType)
            .Where(item => item.UserId == userId && item.Date == date)
            .ToDictionaryAsync(item => item.Date, cancellationToken);

        var leaveRequests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(request => request.LeaveType)
            .Where(request =>
                request.RequesterUserId == userId
                && request.Status == ApprovalStatuses.Approved
                && request.StartDate <= date
                && request.EndDate >= date)
            .ToListAsync(cancellationToken);

        return ResolveDay(userId, userId, date, templates, overrides, leaveRequests);
    }

    private static ResolvedScheduleDayDto ResolveDay(
        string userId,
        string displayName,
        DateOnly date,
        IReadOnlyList<ScheduleTemplate> templates,
        IReadOnlyDictionary<DateOnly, ScheduleDayOverride> overrides,
        IReadOnlyList<LeaveRequest> leaveRequests)
    {
        var leave = leaveRequests.FirstOrDefault(request =>
            request.StartDate <= date && request.EndDate >= date);
        if (leave is not null)
        {
            return new ResolvedScheduleDayDto(
                userId,
                displayName,
                date.ToString("yyyy-MM-dd"),
                PlannedAttendanceKinds.OnLeave,
                null,
                null,
                null,
                null,
                false,
                leave.Id.ToString(),
                leave.LeaveType.Name,
                false);
        }

        if (overrides.TryGetValue(date, out var dayOverride))
        {
            return new ResolvedScheduleDayDto(
                userId,
                displayName,
                date.ToString("yyyy-MM-dd"),
                PlannedAttendanceKinds.Work,
                dayOverride.LocationTypeId,
                dayOverride.LocationType.Code,
                dayOverride.LocationType.Name,
                dayOverride.LocationType.Color,
                dayOverride.LocationType.TracksCollaborators,
                null,
                null,
                true);
        }

        var template = templates.FirstOrDefault(item =>
            item.EffectiveFrom <= date
            && (item.EffectiveTo is null || item.EffectiveTo >= date));

        var templateDay = template?.Days.FirstOrDefault(day => day.DayOfWeek == (int)date.DayOfWeek);
        if (templateDay?.LocationType is not null)
        {
            return new ResolvedScheduleDayDto(
                userId,
                displayName,
                date.ToString("yyyy-MM-dd"),
                PlannedAttendanceKinds.Work,
                templateDay.LocationTypeId,
                templateDay.LocationType.Code,
                templateDay.LocationType.Name,
                templateDay.LocationType.Color,
                templateDay.LocationType.TracksCollaborators,
                null,
                null,
                false);
        }

        return new ResolvedScheduleDayDto(
            userId,
            displayName,
            date.ToString("yyyy-MM-dd"),
            PlannedAttendanceKinds.Unscheduled,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            false);
    }

    private async Task<string> ResolveTargetUserIdAsync(
        UserDataScope scope,
        string actingUserId,
        string? requestedUserId,
        bool forWrite,
        CancellationToken cancellationToken)
    {
        var targetUserId = string.IsNullOrWhiteSpace(requestedUserId) ? actingUserId : requestedUserId.Trim();

        if (forWrite)
        {
            if (!scope.CanManageUserAttendance(actingUserId, targetUserId))
            {
                throw new UnauthorizedAccessException("You cannot manage schedule or attendance for this user.");
            }

            return targetUserId;
        }

        var viewerProfile = await orgDirectory.GetStaffOrgInfoAsync(actingUserId, cancellationToken);
        var targetProfile = await orgDirectory.GetStaffOrgInfoAsync(targetUserId, cancellationToken);
        if (!LeaveVisibility.CanViewUser(scope, actingUserId, viewerProfile, targetUserId, targetProfile))
        {
            throw new UnauthorizedAccessException("You cannot view schedule or attendance for this user.");
        }

        return targetUserId;
    }

    private async Task<IReadOnlyList<string>> ResolveVisibleUserIdsAsync(
        UserDataScope scope,
        string actingUserId,
        StaffOrgInfo? viewerProfile,
        string? targetUserId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(targetUserId))
        {
            var single = await ResolveTargetUserIdAsync(scope, actingUserId, targetUserId, forWrite: false, cancellationToken);
            return [single];
        }

        if (scope.BypassRowLevelSecurity
            || scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase)))
        {
            var staff = await orgDirectory.GetActiveStaffForAccrualAsync(cancellationToken);
            return staff.Select(item => item.UserId).Distinct(StringComparer.Ordinal).ToList();
        }

        if (scope.IsManagerApprover && scope.ReportUserIds.Count > 0)
        {
            return scope.ReportUserIds
                .Append(actingUserId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        return [actingUserId];
    }

    private static string ResolveAttendanceSource(UserDataScope scope, string actingUserId, string targetUserId)
    {
        if (string.Equals(actingUserId, targetUserId, StringComparison.Ordinal))
        {
            return AttendanceSources.Self;
        }

        if (scope.BypassRowLevelSecurity)
        {
            return AttendanceSources.Admin;
        }

        if (scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase)))
        {
            return AttendanceSources.Hr;
        }

        return AttendanceSources.Manager;
    }

    private async Task<Dictionary<string, string>> ResolveDisplayNamesAsync(
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

    private static WorkLocationTypeDto MapLocation(WorkLocationType type) =>
        new(type.Id, type.Code, type.Name, type.Color, type.TracksCollaborators, type.IsActive, type.SortOrder);

    private static ScheduleTemplateDto MapTemplate(
        ScheduleTemplate template,
        IReadOnlyDictionary<string, string> displayNames) =>
        new(
            template.Id,
            template.UserId,
            displayNames.GetValueOrDefault(template.UserId) ?? template.UserId,
            template.EffectiveFrom.ToString("yyyy-MM-dd"),
            template.EffectiveTo?.ToString("yyyy-MM-dd"),
            template.Notes,
            template.Days
                .OrderBy(day => day.DayOfWeek == 0 ? 7 : day.DayOfWeek)
                .Select(day => new ScheduleTemplateDayDto(
                    day.DayOfWeek,
                    day.LocationTypeId,
                    day.LocationType.Name,
                    day.LocationType.Color))
                .ToList());

    private static ScheduleDayOverrideDto MapOverride(ScheduleDayOverride item) =>
        new(
            item.Id,
            item.UserId,
            item.Date.ToString("yyyy-MM-dd"),
            item.LocationTypeId,
            item.LocationType.Name,
            item.LocationType.Color,
            item.Notes);

    private static AttendanceDayDto MapAttendance(
        AttendanceDay item,
        IReadOnlyDictionary<string, string> displayNames) =>
        new(
            item.Id,
            item.UserId,
            displayNames.GetValueOrDefault(item.UserId) ?? item.UserId,
            item.Date.ToString("yyyy-MM-dd"),
            item.PlannedLocationTypeId,
            item.PlannedLocationType?.Name,
            item.PlannedLocationType?.Color,
            item.ActualLocationTypeId,
            item.ActualLocationType.Name,
            item.ActualLocationType.Color,
            item.ActualLocationType.TracksCollaborators,
            item.Source,
            item.Notes,
            item.RecordedByUserId,
            displayNames.GetValueOrDefault(item.RecordedByUserId) ?? item.RecordedByUserId,
            item.RecordedAt.ToString("O"),
            item.Collaborators.Select(collaborator => new AttendanceCollaboratorDto(
                collaborator.CollaboratorUserId,
                collaborator.CollaboratorUserId is null
                    ? null
                    : displayNames.GetValueOrDefault(collaborator.CollaboratorUserId),
                collaborator.ExternalName)).ToList());

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();

    private static string NormalizeColor(string? color) =>
        string.IsNullOrWhiteSpace(color) ? "#605E5C" : color.Trim();
}
