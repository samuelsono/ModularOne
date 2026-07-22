using System.Security.Claims;
using System.Text.Json;
using CarTrack.Api;
using CarTrack.Identity.Contracts;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using CarTrack.Server.Users.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Leave;
public static class LeaveEndpoints
{
    public static RouteGroupBuilder MapLeaveEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/types", GetActiveTypesAsync)
            .RequirePermission("leave.requests.read");
        group.MapGet("/requests", GetMyRequestsAsync)
            .RequirePermission("leave.requests.read");
        group.MapGet("/requests/{id:guid}", GetRequestAsync)
            .RequirePermission("leave.requests.read");
        group.MapPost("/requests", CreateRequestAsync)
            .RequirePermission("leave.requests.write")
            .DisableAntiforgery();
        group.MapPost("/requests/{id:guid}/cancel", CancelRequestAsync)
            .RequireAnyPermission("leave.requests.write", "leave.approvals.write");
        group.MapPost("/requests/{id:guid}/document", UploadDocumentAsync)
            .RequirePermission("leave.requests.write")
            .DisableAntiforgery();
        group.MapGet("/requests/{id:guid}/document", DownloadDocumentAsync)
            .RequirePermission("leave.requests.read");
        group.MapGet("/approvals/pending", GetPendingApprovalsAsync)
            .RequirePermission("leave.approvals.read");
        group.MapPost("/approvals/{id:guid}/decide", DecideAsync)
            .RequirePermission("leave.approvals.write");
        group.MapGet("/admin/types", GetAdminTypesAsync)
            .RequirePermission("leave.policies.read");
        group.MapPost("/admin/types", CreateTypeAsync)
            .RequirePermission("leave.policies.write");
        group.MapPut("/admin/types/{id:guid}", UpdateTypeAsync)
            .RequirePermission("leave.policies.write");
        group.MapDelete("/admin/types/{id:guid}", DeleteTypeAsync)
            .RequirePermission("leave.policies.write");
        group.MapGet("/admin/holidays", GetAdminHolidaysAsync)
            .RequirePermission("leave.policies.read");
        group.MapGet("/holidays/upcoming", GetUpcomingHolidaysAsync)
            .RequireAuthorization();
        group.MapPost("/admin/holidays", CreateHolidayAsync)
            .RequirePermission("leave.policies.write");
        group.MapPut("/admin/holidays/{id:guid}", UpdateHolidayAsync)
            .RequirePermission("leave.policies.write");
        group.MapDelete("/admin/holidays/{id:guid}", DeleteHolidayAsync)
            .RequirePermission("leave.policies.write");
        group.MapPost("/admin/holidays/sync", SyncHolidaysAsync)
            .RequirePermission("leave.policies.write");
        group.MapGet("/working-days", GetWorkingDaysAsync)
            .RequirePermission("leave.requests.read");
        group.MapGet("/balances", GetMyBalancesAsync)
            .RequirePermission("leave.balances.read");
        group.MapGet("/balances/{userId}", GetUserBalancesAsync)
            .RequirePermission("leave.balances.read");
        group.MapPost("/admin/balances/adjust", AdjustBalanceAsync)
            .RequirePermission("leave.balances.write")
            .RequireAuthorization(policy => policy.RequireRole(
                AppRoles.Admin,
                AppRoles.SystemAdmin,
                AppRoles.Hr));
        group.MapPost("/admin/accrual/run", RunAccrualAsync)
            .RequirePermission("leave.policies.write");
        group.MapGet("/calendar", GetCalendarAsync)
            .RequirePermission("leave.calendar.read");
        group.MapLeaveReportEndpoints();
        group.MapAttendanceScheduleEndpoints();
        return group;
    }

    private static async Task<IResult> GetActiveTypesAsync(
        string? forUserId,
        ILeaveConfigurationService leaveConfigurationService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var targetUserId = userId;
        if (!string.IsNullOrWhiteSpace(forUserId)
            && !string.Equals(forUserId, userId, StringComparison.Ordinal))
        {
            if (!IsLeaveBalanceAdministrator(principal))
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "Only administrators and HR can load leave types for another employee.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            targetUserId = forUserId;
        }

        var items = await leaveConfigurationService.GetActiveTypesAsync(targetUserId, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetMyRequestsAsync(
        string? status,
        ILeaveApprovalService leaveApprovalService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        var items = await leaveApprovalService.GetMyRequestsAsync(userId, status, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetRequestAsync(
        Guid id,
        ILeaveApprovalService leaveApprovalService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        try
        {
            var item = await leaveApprovalService.GetRequestAsync(id, userId, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> CreateRequestAsync(
        HttpRequest httpRequest,
        ILeaveApprovalService leaveApprovalService,
        ILeaveLifecycleNotifier leaveLifecycleNotifier,
        ISecurityAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILoggerFactory loggerFactory,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(LeaveEndpoints));
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        CreateLeaveRequest createRequest;
        IFormFile? document = null;
        if (httpRequest.HasFormContentType)
        {
            var form = await httpRequest.ReadFormAsync(cancellationToken);
            if (!Guid.TryParse(form["leaveTypeId"], out var leaveTypeId))
            {
                return Results.Problem(
                    title: "Invalid leave type",
                    detail: "leaveTypeId is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            var startDate = form["startDate"].ToString();
            var endDate = form["endDate"].ToString();
            if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
            {
                return Results.Problem(
                    title: "Invalid dates",
                    detail: "startDate and endDate are required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            createRequest = new CreateLeaveRequest(
                leaveTypeId,
                startDate,
                endDate,
                string.IsNullOrWhiteSpace(form["notes"]) ? null : form["notes"].ToString(),
                string.IsNullOrWhiteSpace(form["startDayPortion"]) ? null : form["startDayPortion"].ToString(),
                string.IsNullOrWhiteSpace(form["endDayPortion"]) ? null : form["endDayPortion"].ToString(),
                string.IsNullOrWhiteSpace(form["onBehalfOfUserId"]) ? null : form["onBehalfOfUserId"].ToString());
            document = form.Files.GetFile("document");
        }
        else
        {
            var request = await httpRequest.ReadFromJsonAsync<CreateLeaveRequest>(cancellationToken);
            if (request is null)
            {
                return Results.Problem(
                    title: "Invalid request",
                    detail: "Request body is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            createRequest = request;
        }

        var requesterUserId = userId;
        if (!string.IsNullOrWhiteSpace(createRequest.OnBehalfOfUserId)
            && !string.Equals(createRequest.OnBehalfOfUserId, userId, StringComparison.Ordinal))
        {
            if (!IsLeaveBalanceAdministrator(principal))
            {
                return Results.Problem(
                    title: "Forbidden",
                    detail: "Only administrators and HR can submit leave on behalf of another employee.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var targetUser = await userManager.FindByIdAsync(createRequest.OnBehalfOfUserId);
            if (targetUser is null || !targetUser.IsActive)
            {
                return Results.Problem(
                    title: "Invalid employee",
                    detail: "The selected employee was not found or is inactive.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            requesterUserId = targetUser.Id;
        }

        try
        {
            var created = await leaveApprovalService.CreateRequestAsync(
                requesterUserId,
                createRequest,
                document,
                cancellationToken);
            var onBehalfSuffix = string.Equals(requesterUserId, userId, StringComparison.Ordinal)
                ? string.Empty
                : $" (submitted by {userId} on behalf of {requesterUserId})";
            await auditService.LogAsync(
                "leave.request.created",
                created.RequesterUserId,
                created.RequesterDisplayName,
                $"Leave request {created.Id} ({created.LeaveType}, {created.StartDate} to {created.EndDate}){onBehalfSuffix}",
                cancellationToken);
            await TryNotifyAsync(
                () => leaveLifecycleNotifier.NotifyLeaveSubmittedAsync(
                    created.ManagerUserId,
                    created.RequesterDisplayName,
                    created.LeaveType,
                    created.StartDate,
                    created.EndDate,
                    created.Id,
                    cancellationToken),
                logger);
            return Results.Created($"/api/leave/requests/{created.Id}", created);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid leave request JSON body.");
            return Results.Problem(
                title: "Invalid request",
                detail: "The leave request body could not be read. Check leave type and dates.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation("Leave request rejected: {Detail}", ex.Message);
            return Results.Problem(
                title: "Unable to create leave request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CancelRequestAsync(
        Guid id,
        CancelLeaveRequest request,
        ILeaveApprovalService leaveApprovalService,
        ILeaveLifecycleNotifier leaveLifecycleNotifier,
        ISecurityAuditService auditService,
        ILoggerFactory loggerFactory,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(LeaveEndpoints));
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        try
        {
            var cancelled = await leaveApprovalService.CancelRequestAsync(id, userId, request, cancellationToken);
            if (cancelled is null)
            {
                return Results.NotFound();
            }
            await auditService.LogAsync(
                "leave.request.cancelled",
                cancelled.RequesterUserId,
                cancelled.RequesterDisplayName,
                $"Leave request {cancelled.Id} cancelled",
                cancellationToken);
            await TryNotifyAsync(
                () => leaveLifecycleNotifier.NotifyLeaveCancelledAsync(
                    cancelled.ManagerUserId,
                    cancelled.RequesterDisplayName,
                    cancelled.LeaveType,
                    cancelled.StartDate,
                    cancelled.EndDate,
                    cancelled.Id,
                    cancellationToken),
                logger);
            return Results.Ok(cancelled);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to cancel leave request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UploadDocumentAsync(
        Guid id,
        IFormFile document,
        ILeaveApprovalService leaveApprovalService,
        ISecurityAuditService auditService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        if (document is null || document.Length == 0)
        {
            return Results.Problem(
                title: "Invalid document",
                detail: "A document file is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        try
        {
            var updated = await leaveApprovalService.UploadDocumentAsync(id, userId, document, cancellationToken);
            if (updated is null)
            {
                return Results.NotFound();
            }
            await auditService.LogAsync(
                "leave.request.document.uploaded",
                updated.RequesterUserId,
                updated.RequesterDisplayName,
                $"Document uploaded for leave request {updated.Id}",
                cancellationToken);
            return Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to upload document",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DownloadDocumentAsync(
        Guid id,
        ILeaveApprovalService leaveApprovalService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        try
        {
            var document = await leaveApprovalService.GetDocumentAsync(id, userId, cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }
            return Results.File(document.Value.Stream, document.Value.ContentType, document.Value.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> GetPendingApprovalsAsync(
        ILeaveApprovalService leaveApprovalService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        var items = await leaveApprovalService.GetPendingApprovalsAsync(userId, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> DecideAsync(
        Guid id,
        ApprovalDecisionRequest request,
        ILeaveApprovalService leaveApprovalService,
        ILeaveLifecycleNotifier leaveLifecycleNotifier,
        ISecurityAuditService auditService,
        ILoggerFactory loggerFactory,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(LeaveEndpoints));
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        try
        {
            var updated = await leaveApprovalService.DecideAsync(id, userId, request, cancellationToken);
            if (updated is null)
            {
                return Results.NotFound();
            }
            await auditService.LogAsync(
                request.Approve ? "leave.request.approved" : "leave.request.rejected",
                updated.RequesterUserId,
                updated.RequesterDisplayName,
                $"Leave request {updated.Id} ({updated.LeaveType}, {updated.StartDate} to {updated.EndDate})",
                cancellationToken);
            await TryNotifyAsync(
                () => request.Approve
                    ? leaveLifecycleNotifier.NotifyLeaveApprovedAsync(
                    updated.RequesterUserId,
                        updated.LeaveType,
                        updated.StartDate,
                        updated.EndDate,
                        updated.Id,
                        cancellationToken)
                    : leaveLifecycleNotifier.NotifyLeaveRejectedAsync(
                    updated.RequesterUserId,
                        updated.LeaveType,
                        updated.StartDate,
                        updated.EndDate,
                        updated.Id,
                        cancellationToken),
                logger);
            return Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to decide leave request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetAdminTypesAsync(
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        var items = await leaveConfigurationService.GetAdminTypesAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateTypeAsync(
        SaveLeaveTypeRequest request,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await leaveConfigurationService.CreateTypeAsync(request, cancellationToken);
            return Results.Created($"/api/leave/admin/types/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create leave type",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateTypeAsync(
        Guid id,
        SaveLeaveTypeRequest request,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await leaveConfigurationService.UpdateTypeAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update leave type",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteTypeAsync(
        Guid id,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await leaveConfigurationService.DeleteTypeAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to delete leave type",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetAdminHolidaysAsync(
        int? year,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        var items = await leaveConfigurationService.GetAdminHolidaysAsync(year, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetUpcomingHolidaysAsync(
        int? untilYear,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        var targetYear = untilYear ?? DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var items = await leaveConfigurationService.GetUpcomingHolidaysAsync(targetYear, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateHolidayAsync(
        SavePublicHolidayRequest request,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await leaveConfigurationService.CreateHolidayAsync(request, cancellationToken);
            return Results.Created($"/api/leave/admin/holidays/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create public holiday",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateHolidayAsync(
        Guid id,
        SavePublicHolidayRequest request,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await leaveConfigurationService.UpdateHolidayAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update public holiday",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteHolidayAsync(
        Guid id,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        var deleted = await leaveConfigurationService.DeleteHolidayAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> SyncHolidaysAsync(
        int? year,
        ILeaveConfigurationService leaveConfigurationService,
        CancellationToken cancellationToken)
    {
        var added = await leaveConfigurationService.SyncHolidaysAsync(year, cancellationToken);
        return Results.Ok(new { added });
    }

    private static async Task<IResult> GetWorkingDaysAsync(
        string startDate,
        string endDate,
        string? startDayPortion,
        string? endDayPortion,
        ILeaveWorkingDaysService leaveWorkingDaysService,
        IOrgDirectory orgDirectory,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
        {
            return Results.Problem(
                title: "Invalid dates",
                detail: "Start and end dates must be valid.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        string? branch = null;
        var userId = GetUserId(principal);
        if (userId is not null)
        {
            branch = await orgDirectory.ResolveRequesterBranchAsync(
                userId,
                cancellationToken);
        }
        try
        {
            var result = await leaveWorkingDaysService.CalculateAsync(
                start,
                end,
                branch,
                startDayPortion,
                endDayPortion,
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to calculate working days",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetMyBalancesAsync(
        int? year,
        ILeaveBalanceService leaveBalanceService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }
        var items = await leaveBalanceService.GetMyBalancesAsync(userId, year, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetUserBalancesAsync(
        string userId,
        int? year,
        ILeaveBalanceService leaveBalanceService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(principal);
        if (currentUserId is null)
        {
            return Results.Unauthorized();
        }
        var isSelf = string.Equals(currentUserId, userId, StringComparison.Ordinal);
        var canViewOthers = IsLeaveBalanceAdministrator(principal);
        if (!isSelf && !canViewOthers)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "You can only view your own leave balances.",
                statusCode: StatusCodes.Status403Forbidden);
        }
        var items = await leaveBalanceService.GetUserBalancesAsync(userId, year, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> AdjustBalanceAsync(
        AdjustLeaveBalanceRequest request,
        ILeaveBalanceService leaveBalanceService,
        ISecurityAuditService auditService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!IsLeaveBalanceAdministrator(principal))
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "Only administrators and HR can adjust leave balances.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        try
        {
            var updated = await leaveBalanceService.AdjustBalanceAsync(request, cancellationToken);
            await auditService.LogAsync(
                "leave.balance.adjusted",
                updated.UserId,
                details: $"Leave balance adjusted for {updated.LeaveTypeName} ({updated.CycleStart} to {updated.CycleEnd})",
                cancellationToken: cancellationToken);
            return Results.Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to adjust leave balance",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static bool IsLeaveBalanceAdministrator(ClaimsPrincipal principal) =>
        principal.IsInRole(AppRoles.Admin)
        || principal.IsInRole(AppRoles.SystemAdmin)
        || principal.IsInRole(AppRoles.Hr)
        || principal.FindAll(ClaimTypes.Role).Any(claim =>
            claim.Value.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            || claim.Value.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || claim.Value.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase))
        || principal.FindAll("role").Any(claim =>
            claim.Value.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            || claim.Value.Equals(AppRoles.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            || claim.Value.Equals(AppRoles.Hr, StringComparison.OrdinalIgnoreCase));

    private static async Task<IResult> RunAccrualAsync(
        ILeaveAccrualService leaveAccrualService,
        CancellationToken cancellationToken)
    {
        var applied = await leaveAccrualService.RunMonthlyAccrualAsync(cancellationToken);
        return Results.Ok(new { applied });
    }

    private static async Task<IResult> GetCalendarAsync(
        string start,
        string end,
        string? branch,
        string? department,
        string? userId,
        ILeaveCalendarService leaveCalendarService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var viewerUserId = GetUserId(principal);
        if (viewerUserId is null)
        {
            return Results.Unauthorized();
        }
        try
        {
            var calendar = await leaveCalendarService.GetCalendarAsync(
                viewerUserId,
                new LeaveCalendarFilters(start, end, branch, department, userId),
                cancellationToken);
            return Results.Ok(calendar);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to load leave calendar",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static async Task TryNotifyAsync(Func<Task> action, ILogger logger)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch leave notification.");
        }
    }
}
