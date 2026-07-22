using System.Security.Claims;
using CarTrack.Api;
using CarTrack.Server.Users;
using CarTrack.Server.Users.Authorization;

namespace CarTrack.Modules.Leave;

public static class AttendanceScheduleEndpoints
{
    public static RouteGroupBuilder MapAttendanceScheduleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/locations", GetLocationsAsync)
            .RequireAnyPermission("leave.schedule.read", "leave.attendance.read", "leave.policies.read");
        group.MapPost("/locations", CreateLocationAsync)
            .RequirePermission("leave.policies.write");
        group.MapPut("/locations/{id:guid}", UpdateLocationAsync)
            .RequirePermission("leave.policies.write");

        group.MapGet("/schedule/templates", GetTemplatesAsync)
            .RequirePermission("leave.schedule.read");
        group.MapPut("/schedule/templates", UpsertTemplateAsync)
            .RequirePermission("leave.schedule.write");
        group.MapGet("/schedule/overrides", GetOverridesAsync)
            .RequirePermission("leave.schedule.read");
        group.MapPut("/schedule/overrides", UpsertOverrideAsync)
            .RequirePermission("leave.schedule.write");
        group.MapDelete("/schedule/overrides/{id:guid}", DeleteOverrideAsync)
            .RequirePermission("leave.schedule.write");
        group.MapGet("/schedule/resolved", GetResolvedAsync)
            .RequirePermission("leave.schedule.read");

        group.MapGet("/attendance", GetAttendanceAsync)
            .RequirePermission("leave.attendance.read");
        group.MapPut("/attendance/{date}", UpsertAttendanceAsync)
            .RequirePermission("leave.attendance.write");
        group.MapGet("/attendance/compare", GetCompareAsync)
            .RequireAnyPermission("leave.attendance.read", "leave.schedule.read");
        group.MapGet("/attendance/policy", GetAttendancePolicyAsync)
            .RequireAnyPermission("leave.attendance.read", "leave.policies.read");
        group.MapPut("/attendance/policy", UpdateAttendancePolicyAsync)
            .RequirePermission("leave.policies.write");

        return group;
    }

    private static async Task<IResult> GetLocationsAsync(
        bool? activeOnly,
        IAttendanceScheduleService service,
        CancellationToken cancellationToken)
    {
        var items = await service.GetLocationTypesAsync(activeOnly ?? true, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateLocationAsync(
        SaveWorkLocationTypeRequest request,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateLocationTypeAsync(request, cancellationToken);
            await auditService.LogAsync(
                "leave.location.created",
                details: $"Work location {created.Code} created",
                cancellationToken: cancellationToken);
            return Results.Ok(created);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            return Results.Problem(
                title: "Unable to create location type",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateLocationAsync(
        Guid id,
        SaveWorkLocationTypeRequest request,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await service.UpdateLocationTypeAsync(id, request, cancellationToken);
            if (updated is null)
            {
                return Results.NotFound();
            }

            await auditService.LogAsync(
                "leave.location.updated",
                details: $"Work location {updated.Code} updated",
                cancellationToken: cancellationToken);
            return Results.Ok(updated);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            return Results.Problem(
                title: "Unable to update location type",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetTemplatesAsync(
        string? userId,
        IAttendanceScheduleService service,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var items = await service.GetTemplatesAsync(actingUserId, userId, cancellationToken);
            return Results.Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> UpsertTemplateAsync(
        UpsertScheduleTemplateRequest request,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var saved = await service.UpsertTemplateAsync(actingUserId, request, cancellationToken);
            await auditService.LogAsync(
                "leave.schedule.updated",
                saved.UserId,
                saved.UserDisplayName,
                $"Schedule template effective {saved.EffectiveFrom}",
                cancellationToken);
            return Results.Ok(saved);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Unable to save schedule", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetOverridesAsync(
        string? userId,
        string? from,
        string? to,
        IAttendanceScheduleService service,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            DateOnly? fromDate = null;
            DateOnly? toDate = null;
            if (!string.IsNullOrWhiteSpace(from))
            {
                if (!DateOnly.TryParse(from, out var parsedFrom))
                {
                    return Results.Problem(title: "Invalid from date", statusCode: StatusCodes.Status400BadRequest);
                }

                fromDate = parsedFrom;
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                if (!DateOnly.TryParse(to, out var parsedTo))
                {
                    return Results.Problem(title: "Invalid to date", statusCode: StatusCodes.Status400BadRequest);
                }

                toDate = parsedTo;
            }

            var items = await service.GetOverridesAsync(actingUserId, userId, fromDate, toDate, cancellationToken);
            return Results.Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> UpsertOverrideAsync(
        UpsertScheduleDayOverrideRequest request,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var saved = await service.UpsertOverrideAsync(actingUserId, request, cancellationToken);
            await auditService.LogAsync(
                "leave.schedule.override.updated",
                saved.UserId,
                details: $"Schedule override on {saved.Date}",
                cancellationToken: cancellationToken);
            return Results.Ok(saved);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Unable to save override", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteOverrideAsync(
        Guid id,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var deleted = await service.DeleteOverrideAsync(actingUserId, id, cancellationToken);
            if (!deleted)
            {
                return Results.NotFound();
            }

            await auditService.LogAsync(
                "leave.schedule.override.deleted",
                details: $"Schedule override {id} deleted",
                cancellationToken: cancellationToken);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> GetResolvedAsync(
        string from,
        string to,
        string? userId,
        IAttendanceScheduleService service,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
        {
            return Results.Problem(title: "Invalid date range", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var items = await service.GetResolvedScheduleAsync(actingUserId, fromDate, toDate, userId, cancellationToken);
            return Results.Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Unable to resolve schedule", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetAttendanceAsync(
        string from,
        string to,
        string? userId,
        IAttendanceScheduleService service,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
        {
            return Results.Problem(title: "Invalid date range", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var items = await service.GetAttendanceAsync(actingUserId, fromDate, toDate, userId, cancellationToken);
            return Results.Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Unable to load attendance", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpsertAttendanceAsync(
        string date,
        UpsertAttendanceDayRequest request,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        if (!DateOnly.TryParse(date, out var day))
        {
            return Results.Problem(title: "Invalid date", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var saved = await service.UpsertAttendanceAsync(actingUserId, day, request, cancellationToken);
            await auditService.LogAsync(
                "leave.attendance.recorded",
                saved.UserId,
                saved.UserDisplayName,
                $"Attendance recorded for {saved.Date} as {saved.ActualLocationTypeName}",
                cancellationToken);
            return Results.Ok(saved);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Unable to save attendance", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetCompareAsync(
        string from,
        string to,
        string? userId,
        IAttendanceScheduleService service,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var actingUserId = GetUserId(principal);
        if (actingUserId is null)
        {
            return Results.Unauthorized();
        }

        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
        {
            return Results.Problem(title: "Invalid date range", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var items = await service.GetCompareAsync(actingUserId, fromDate, toDate, userId, cancellationToken);
            return Results.Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Unable to compare attendance", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetAttendancePolicyAsync(
        IAttendanceScheduleService service,
        CancellationToken cancellationToken)
    {
        var settings = await service.GetAttendancePolicyAsync(cancellationToken);
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateAttendancePolicyAsync(
        UpdateAttendancePolicySettingsRequest request,
        IAttendanceScheduleService service,
        ISecurityAuditService auditService,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await service.UpdateAttendancePolicyAsync(request, cancellationToken);
            await auditService.LogAsync(
                "leave.attendance.policy.updated",
                details: $"Attendance default assumption set to {settings.DefaultAssumption}",
                cancellationToken: cancellationToken);
            return Results.Ok(settings);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update attendance policy",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
