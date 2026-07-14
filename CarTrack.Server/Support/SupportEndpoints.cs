using System.Security.Claims;
using CarTrack.Server.Data;
using CarTrack.Server.Users;
using CarTrack.Server.Users.Authorization;

namespace CarTrack.Server.Support;

public static class SupportEndpoints
{
    private const string AdminWritePermission = "platform.support.write";

    public static RouteGroupBuilder MapSupportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/categories", GetCategoriesAsync)
            .RequireAuthorization();

        group.MapPost("/tickets", SubmitTicketAsync)
            .RequireAuthorization();

        group.MapGet("/tickets", GetMyTicketsAsync)
            .RequireAuthorization();

        group.MapGet("/tickets/{id:guid}", GetTicketAsync)
            .RequireAuthorization();

        group.MapGet("/admin/tickets", GetAllTicketsAdminAsync)
            .RequirePermission(AdminWritePermission);

        group.MapPut("/admin/tickets/{id:guid}", UpdateTicketAdminAsync)
            .RequirePermission(AdminWritePermission);

        group.MapDelete("/admin/tickets/{id:guid}", DeleteTicketAdminAsync)
            .RequirePermission(AdminWritePermission);

        group.MapGet("/admin/categories", GetAdminCategoriesAsync)
            .RequirePermission(AdminWritePermission);

        group.MapPost("/admin/categories", CreateCategoryAsync)
            .RequirePermission(AdminWritePermission);

        group.MapPut("/admin/categories/{id}", UpdateCategoryAsync)
            .RequirePermission(AdminWritePermission);

        group.MapDelete("/admin/categories/{id}", DeleteCategoryAsync)
            .RequirePermission(AdminWritePermission);

        return group;
    }

    private static async Task<IResult> GetCategoriesAsync(
        TicketType? type,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        var categories = await supportService.GetCategoriesAsync(type, includeInactive: false, cancellationToken);
        return Results.Ok(categories);
    }

    private static async Task<IResult> SubmitTicketAsync(
        SubmitTicketRequest request,
        ISupportService supportService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var ticket = await supportService.SubmitTicketAsync(userId, request, cancellationToken);
            return Results.Created($"/api/support/tickets/{ticket.Id}", ticket);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to submit ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetMyTicketsAsync(
        ISupportService supportService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var tickets = await supportService.GetMyTicketsAsync(userId, cancellationToken);
        return Results.Ok(tickets);
    }

    private static async Task<IResult> GetTicketAsync(
        Guid id,
        ISupportService supportService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var ticket = await supportService.GetTicketAsync(id, userId, IsAdmin(user), cancellationToken);
            return ticket is null ? Results.NotFound() : Results.Ok(ticket);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> GetAllTicketsAdminAsync(
        TicketType? type,
        TicketStatus? status,
        TicketPriority? priority,
        string? search,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        var tickets = await supportService.GetAllTicketsAdminAsync(
            new SupportTicketFilters(type, status, priority, search),
            cancellationToken);
        return Results.Ok(tickets);
    }

    private static async Task<IResult> UpdateTicketAdminAsync(
        Guid id,
        UpdateTicketAdminRequest request,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await supportService.UpdateTicketAdminAsync(id, request, cancellationToken);
            return ticket is null ? Results.NotFound() : Results.Ok(ticket);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update ticket",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteTicketAdminAsync(
        Guid id,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        var deleted = await supportService.DeleteTicketAdminAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetAdminCategoriesAsync(
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        var categories = await supportService.GetCategoriesAsync(type: null, includeInactive: true, cancellationToken);
        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateCategoryAsync(
        SaveCategoryRequest request,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await supportService.CreateCategoryAsync(request, cancellationToken);
            return Results.Created($"/api/support/admin/categories/{category.Id}", category);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create category",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateCategoryAsync(
        string id,
        SaveCategoryRequest request,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await supportService.UpdateCategoryAsync(id, request, cancellationToken);
            return category is null ? Results.NotFound() : Results.Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update category",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteCategoryAsync(
        string id,
        ISupportService supportService,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await supportService.DeleteCategoryAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to delete category",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

    private static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.SystemAdmin);
}
