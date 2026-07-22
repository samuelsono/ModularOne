using CarTrack.Server.Data;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;

namespace CarTrack.Modules.Leave;

public class LeaveApprovalService(
    LeaveDbContext dbContext,
    ICurrentUserScope currentUserScope,
    IOrgDirectory orgDirectory,
    UserManager<ApplicationUser> userManager,
    ILeaveWorkingDaysService leaveWorkingDaysService,
    ILeaveBalanceStore leaveBalanceStore,
    ILeaveDocumentStorage leaveDocumentStorage) : ILeaveApprovalService
{
    public async Task<LeaveRequestDto> CreateRequestAsync(
        string requesterUserId,
        CreateLeaveRequest request,
        IFormFile? document = null,
        CancellationToken cancellationToken = default)
    {
        var managerUserId = await orgDirectory.ResolveManagerUserIdAsync(
            requesterUserId,
            cancellationToken);

        // Allow submission when no manager is configured so HR/admins (bypass scope)
        // can still action the request. Prefer an assigned manager when present.
        if (string.IsNullOrWhiteSpace(managerUserId))
        {
            managerUserId = requesterUserId;
        }

        if (!DateOnly.TryParse(request.StartDate, out var startDate)
            || !DateOnly.TryParse(request.EndDate, out var endDate))
        {
            throw new InvalidOperationException("Invalid leave dates.");
        }

        if (endDate < startDate)
        {
            throw new InvalidOperationException("Leave end date must be on or after the start date.");
        }

        var leaveType = await dbContext.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == request.LeaveTypeId && type.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The selected leave type is not valid.");

        var requesterBranch = await orgDirectory.ResolveRequesterBranchAsync(
            requesterUserId,
            cancellationToken);

        var startDayPortion = LeaveDayPortions.Normalize(request.StartDayPortion);
        var endDayPortion = LeaveDayPortions.Normalize(request.EndDayPortion);

        if ((string.Equals(startDayPortion, LeaveDayPortions.Half, StringComparison.Ordinal)
                || string.Equals(endDayPortion, LeaveDayPortions.Half, StringComparison.Ordinal))
            && !leaveType.AllowHalfDay)
        {
            throw new InvalidOperationException("Half-day leave is not allowed for this leave type.");
        }

        var workingDaysResult = await leaveWorkingDaysService.CalculateAsync(
            startDate,
            endDate,
            requesterBranch,
            startDayPortion,
            endDayPortion,
            cancellationToken);

        if (workingDaysResult.WorkingDays <= 0)
        {
            throw new InvalidOperationException("The selected date range contains no working days.");
        }

        await ValidateRequestRulesAsync(
            requesterUserId,
            leaveType,
            startDate,
            endDate,
            workingDaysResult.WorkingDays,
            cancellationToken);

        // Supporting documents for types that require them can be uploaded in the same
        // request (multipart) or immediately after via POST /requests/{id}/document.
        // Approval is blocked until a document is attached.

        var entityId = await ExecuteInTransactionAsync(async ct =>
        {
            var entity = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                RequesterUserId = requesterUserId,
                ManagerUserId = managerUserId,
                LeaveTypeId = leaveType.Id,
                StartDate = startDate,
                EndDate = endDate,
                StartDayPortion = startDayPortion,
                EndDayPortion = endDayPortion,
                WorkingDays = workingDaysResult.WorkingDays,
                Status = ApprovalStatuses.Pending,
                RequesterBranch = requesterBranch,
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            if (document is not null)
            {
                var savedDocument = await leaveDocumentStorage.SaveAsync(entity.Id, document, ct);
                entity.DocumentPath = savedDocument.StoredPath;
                entity.DocumentFileName = savedDocument.FileName;
                entity.DocumentContentType = savedDocument.ContentType;
            }

            dbContext.LeaveRequests.Add(entity);

            if (leaveType.DeductsBalance)
            {
                var (cycleStart, cycleEnd) = GetCycleForDate(startDate);
                var balance = await leaveBalanceStore.GetOrCreateBalanceAsync(
                    requesterUserId,
                    leaveType.Id,
                    cycleStart,
                    cycleEnd,
                    ct);

                var remaining = balance.Allocated + balance.Adjusted - balance.Used - balance.Pending;
                if (remaining < workingDaysResult.WorkingDays)
                {
                    throw new InvalidOperationException(
                        $"Insufficient leave balance. Remaining: {remaining:0.##}, requested: {workingDaysResult.WorkingDays:0.##}.");
                }

                balance.Pending += workingDaysResult.WorkingDays;
            }

            return entity.Id;
        }, cancellationToken);

        return await MapAsync(entityId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created leave request.");
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(
        string requesterUserId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var query = dbContext.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .AsQueryable();

        if (!scope.BypassRowLevelSecurity
            && !scope.Roles.Any(role => role.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase)))
        {
            var visibleUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { requesterUserId };
            foreach (var reportUserId in scope.ReportUserIds)
            {
                visibleUserIds.Add(reportUserId);
            }

            query = query.Where(item => visibleUserIds.Contains(item.RequesterUserId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status.Trim());
        }

        var requests = await query
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        await BackfillMissingWorkingDaysAsync(requests, cancellationToken);

        return await MapManyAsync(requests, cancellationToken);
    }

    public async Task<LeaveRequestDto?> GetRequestAsync(
        Guid requestId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var isOwner = string.Equals(entity.RequesterUserId, userId, StringComparison.Ordinal);
        var canViewAsApprover = scope.CanApproveLeave(entity.RequesterUserId, entity.ManagerUserId);

        if (!isOwner && !canViewAsApprover)
        {
            throw new UnauthorizedAccessException("You do not have access to this leave request.");
        }

        await BackfillMissingWorkingDaysAsync([entity], cancellationToken);

        return await MapAsync(entity, cancellationToken);
    }

    public async Task<LeaveRequestDto?> CancelRequestAsync(
        Guid requestId,
        string actingUserId,
        CancelLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Notes))
        {
            throw new InvalidOperationException("A reason is required to cancel a leave request.");
        }

        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.LeaveRequests
            .Include(item => item.LeaveType)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var isRequester = string.Equals(entity.RequesterUserId, actingUserId, StringComparison.Ordinal);
        var canManage = scope.CanApproveLeave(entity.RequesterUserId, entity.ManagerUserId);

        if (entity.Status.Equals(ApprovalStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            if (!isRequester)
            {
                return null;
            }
        }
        else if (entity.Status.Equals(ApprovalStatuses.Approved, StringComparison.OrdinalIgnoreCase))
        {
            if (!isRequester && !canManage)
            {
                return null;
            }

            if (HasLeaveStarted(entity))
            {
                throw new InvalidOperationException("Approved leave can only be cancelled before it starts.");
            }
        }
        else
        {
            throw new InvalidOperationException("Only pending or approved leave requests can be cancelled.");
        }

        var entityId = entity.Id;
        var wasApproved = entity.Status.Equals(ApprovalStatuses.Approved, StringComparison.OrdinalIgnoreCase);
        await ExecuteInTransactionAsync(async ct =>
        {
            entity.Status = ApprovalStatuses.Cancelled;
            entity.DecidedAt = DateTimeOffset.UtcNow;
            entity.DecidedByUserId = actingUserId;
            entity.Notes = request.Notes.Trim();

            if (wasApproved)
            {
                await ReleaseUsedBalanceAsync(entity, ct);
            }
            else
            {
                await ReleasePendingBalanceAsync(entity, ct);
            }
        }, cancellationToken);

        return await MapAsync(entityId, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetPendingApprovalsAsync(
        string managerUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Where(item => item.Status == ApprovalStatuses.Pending)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        await BackfillMissingWorkingDaysAsync(requests, cancellationToken);

        var visible = requests
            .Where(item => scope.CanViewLeaveApproval(item.RequesterUserId, item.RequesterBranch)
                || string.Equals(item.ManagerUserId, managerUserId, StringComparison.Ordinal))
            .ToList();
        return await MapManyAsync(visible, cancellationToken);
    }

    public async Task<LeaveRequestDto?> DecideAsync(
        Guid requestId,
        string managerUserId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.LeaveRequests
            .Include(item => item.LeaveType)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (entity is null || !scope.CanApproveLeave(entity.RequesterUserId, entity.ManagerUserId))
        {
            return null;
        }

        if (!entity.Status.Equals(ApprovalStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Leave request has already been decided.");
        }

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Notes))
        {
            throw new InvalidOperationException("A reason is required to reject a leave request.");
        }

        if (request.Approve && entity.LeaveType.RequiresDocument && string.IsNullOrWhiteSpace(entity.DocumentPath))
        {
            throw new InvalidOperationException("A supporting document is required before this leave can be approved.");
        }

        var entityId = entity.Id;
        await ExecuteInTransactionAsync(async ct =>
        {
            entity.Status = request.Approve ? ApprovalStatuses.Approved : ApprovalStatuses.Rejected;
            entity.DecidedAt = DateTimeOffset.UtcNow;
            entity.DecidedByUserId = managerUserId;
            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                entity.Notes = request.Notes.Trim();
            }

            if (request.Approve)
            {
                await CommitPendingBalanceAsync(entity, ct);
            }
            else
            {
                await ReleasePendingBalanceAsync(entity, ct);
            }
        }, cancellationToken);

        return await MapAsync(entityId, cancellationToken);
    }

    public async Task<LeaveRequestDto?> UploadDocumentAsync(
        Guid requestId,
        string actingUserId,
        IFormFile document,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.LeaveRequests
            .Include(item => item.LeaveType)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (entity is null || !scope.CanUploadLeaveDocument(actingUserId, entity.RequesterUserId))
        {
            return null;
        }

        if (!entity.Status.Equals(ApprovalStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Documents can only be uploaded for pending leave requests.");
        }

        var previousPath = entity.DocumentPath;
        var savedDocument = await leaveDocumentStorage.SaveAsync(entity.Id, document, cancellationToken);
        await leaveDocumentStorage.DeleteIfExistsAsync(previousPath, cancellationToken);

        entity.DocumentPath = savedDocument.StoredPath;
        entity.DocumentFileName = savedDocument.FileName;
        entity.DocumentContentType = savedDocument.ContentType;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapAsync(entity.Id, cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetDocumentAsync(
        Guid requestId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var scope = await currentUserScope.GetAsync(cancellationToken);
        var entity = await dbContext.LeaveRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (entity is null || string.IsNullOrWhiteSpace(entity.DocumentPath))
        {
            return null;
        }

        var orgInfos = await orgDirectory.GetStaffOrgInfoBatchAsync(
            [userId, entity.RequesterUserId],
            cancellationToken);
        var viewerProfile = orgInfos.GetValueOrDefault(userId);
        var requesterProfile = orgInfos.GetValueOrDefault(entity.RequesterUserId);

        if (!LeaveVisibility.CanViewRequest(
                scope,
                userId,
                viewerProfile,
                entity.RequesterUserId,
                requesterProfile))
        {
            throw new UnauthorizedAccessException("You do not have access to this leave document.");
        }

        var opened = await leaveDocumentStorage.OpenReadAsync(entity.DocumentPath, cancellationToken);
        if (opened is null)
        {
            return null;
        }

        var fileName = entity.DocumentFileName ?? opened.Value.FileName;
        var contentType = entity.DocumentContentType ?? opened.Value.ContentType;
        return (opened.Value.Stream, contentType, fileName);
    }

    private async Task ValidateRequestRulesAsync(
        string requesterUserId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        decimal workingDays,
        CancellationToken cancellationToken)
    {
        var staffInfo = await orgDirectory.GetStaffOrgInfoAsync(requesterUserId, cancellationToken);
        if (!LeaveTypeGenderEligibility.IsEligible(leaveType.EligibleGender, staffInfo?.Gender))
        {
            throw new InvalidOperationException("You are not eligible for the selected leave type.");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        // Zero notice means the policy permits retrospective capture. This is
        // required for leave such as sick leave that is commonly submitted
        // after the employee returns. Positive values remain future notice.
        if (leaveType.MinNoticeDays > 0)
        {
            var earliestAllowed = today.AddDays(leaveType.MinNoticeDays);
            if (startDate < earliestAllowed)
            {
                throw new InvalidOperationException(
                    $"This leave type requires at least {leaveType.MinNoticeDays} day(s) notice.");
            }
        }

        if (leaveType.MaxConsecutiveDays is not null && workingDays > leaveType.MaxConsecutiveDays)
        {
            throw new InvalidOperationException(
                $"This leave type allows at most {leaveType.MaxConsecutiveDays} consecutive working day(s).");
        }

        var overlaps = await dbContext.LeaveRequests
            .AsNoTracking()
            .AnyAsync(
                item => item.RequesterUserId == requesterUserId
                    && (item.Status == ApprovalStatuses.Pending || item.Status == ApprovalStatuses.Approved)
                    && item.StartDate <= endDate
                    && item.EndDate >= startDate,
                cancellationToken);

        if (overlaps)
        {
            throw new InvalidOperationException("Leave dates overlap an existing pending or approved request.");
        }
    }

    private async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await action(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private async Task ReleasePendingBalanceAsync(LeaveRequest entity, CancellationToken cancellationToken)
    {
        if (!entity.LeaveType.DeductsBalance)
        {
            return;
        }

        var (cycleStart, cycleEnd) = GetCycleForDate(entity.StartDate);
        var balance = await leaveBalanceStore.GetOrCreateBalanceAsync(
            entity.RequesterUserId,
            entity.LeaveTypeId,
            cycleStart,
            cycleEnd,
            cancellationToken);

        balance.Pending = Math.Max(0m, balance.Pending - entity.WorkingDays);
    }

    private async Task CommitPendingBalanceAsync(LeaveRequest entity, CancellationToken cancellationToken)
    {
        if (!entity.LeaveType.DeductsBalance)
        {
            return;
        }

        var (cycleStart, cycleEnd) = GetCycleForDate(entity.StartDate);
        var balance = await leaveBalanceStore.GetOrCreateBalanceAsync(
            entity.RequesterUserId,
            entity.LeaveTypeId,
            cycleStart,
            cycleEnd,
            cancellationToken);

        balance.Pending = Math.Max(0m, balance.Pending - entity.WorkingDays);
        balance.Used += entity.WorkingDays;
    }

    private async Task ReleaseUsedBalanceAsync(LeaveRequest entity, CancellationToken cancellationToken)
    {
        if (!entity.LeaveType.DeductsBalance)
        {
            return;
        }

        var (cycleStart, cycleEnd) = GetCycleForDate(entity.StartDate);
        var balance = await leaveBalanceStore.GetOrCreateBalanceAsync(
            entity.RequesterUserId,
            entity.LeaveTypeId,
            cycleStart,
            cycleEnd,
            cancellationToken);

        balance.Used = Math.Max(0m, balance.Used - entity.WorkingDays);
    }

    private static bool HasLeaveStarted(LeaveRequest entity) =>
        DateOnly.FromDateTime(DateTime.Now) >= entity.StartDate;

    private static (DateOnly CycleStart, DateOnly CycleEnd) GetCycleForDate(DateOnly date) =>
        (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31));

    private async Task BackfillMissingWorkingDaysAsync(
        IList<LeaveRequest> requests,
        CancellationToken cancellationToken)
    {
        var idsNeedingBackfill = requests
            .Where(request => request.WorkingDays == 0m)
            .Select(request => request.Id)
            .ToList();

        if (idsNeedingBackfill.Count == 0)
        {
            return;
        }

        var tracked = await dbContext.LeaveRequests
            .Where(request => idsNeedingBackfill.Contains(request.Id))
            .ToListAsync(cancellationToken);

        var changed = false;

        foreach (var entity in tracked)
        {
            if (string.IsNullOrWhiteSpace(entity.StartDayPortion))
            {
                entity.StartDayPortion = LeaveDayPortions.Full;
            }

            if (string.IsNullOrWhiteSpace(entity.EndDayPortion))
            {
                entity.EndDayPortion = LeaveDayPortions.Full;
            }

            var result = await leaveWorkingDaysService.CalculateAsync(
                entity.StartDate,
                entity.EndDate,
                entity.RequesterBranch,
                entity.StartDayPortion,
                entity.EndDayPortion,
                cancellationToken);

            if (result.WorkingDays <= 0m)
            {
                continue;
            }

            entity.WorkingDays = result.WorkingDays;
            changed = true;

            var snapshot = requests.FirstOrDefault(request => request.Id == entity.Id);
            if (snapshot is not null)
            {
                snapshot.WorkingDays = entity.WorkingDays;
            }
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<LeaveRequestDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return entity is null ? null : await MapAsync(entity, cancellationToken);
    }

    private async Task<IReadOnlyList<LeaveRequestDto>> MapManyAsync(
        IEnumerable<LeaveRequest> entities,
        CancellationToken cancellationToken)
    {
        var list = entities.ToList();
        var names = await ResolveDisplayNamesAsync(
            list.SelectMany(e => new[] { e.RequesterUserId, e.CreatedByUserId, e.UpdatedByUserId }),
            cancellationToken);
        return list.Select(e => Map(e, names)).ToList();
    }

    private async Task<LeaveRequestDto> MapAsync(LeaveRequest entity, CancellationToken cancellationToken)
    {
        var names = await ResolveDisplayNamesAsync(
            [entity.RequesterUserId, entity.CreatedByUserId, entity.UpdatedByUserId],
            cancellationToken);
        return Map(entity, names);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IEnumerable<string?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

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

    private static string? NameOrNull(IReadOnlyDictionary<string, string> names, string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? null : names.GetValueOrDefault(userId);

    private static LeaveRequestDto Map(LeaveRequest entity, IReadOnlyDictionary<string, string> names) =>
        new(
            entity.Id,
            entity.RequesterUserId,
            names.GetValueOrDefault(entity.RequesterUserId) ?? entity.RequesterUserId,
            entity.ManagerUserId,
            entity.LeaveTypeId,
            entity.LeaveType.Name,
            entity.LeaveType.Color,
            entity.StartDate.ToString("yyyy-MM-dd"),
            entity.EndDate.ToString("yyyy-MM-dd"),
            entity.StartDayPortion,
            entity.EndDayPortion,
            entity.WorkingDays,
            entity.Status,
            entity.Notes,
            !string.IsNullOrWhiteSpace(entity.DocumentPath),
            entity.DocumentFileName,
            AuditableMapping.FormatTimestamp(entity.CreatedAt),
            entity.CreatedByUserId,
            NameOrNull(names, entity.CreatedByUserId),
            AuditableMapping.FormatTimestamp(entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt),
            entity.UpdatedByUserId,
            NameOrNull(names, entity.UpdatedByUserId),
            AuditableMapping.FormatTimestamp(entity.DecidedAt));
}
