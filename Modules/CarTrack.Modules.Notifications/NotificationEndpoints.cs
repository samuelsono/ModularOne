using System.Security.Claims;
using CarTrack.Server.Users; // AppRoles — CarTrack.Identity

namespace CarTrack.Modules.Notifications;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetInboxAsync)
            .RequireAuthorization();

        group.MapGet("/unread-count", GetUnreadCountAsync)
            .RequireAuthorization();

        group.MapPost("/{recipientId:guid}/read", MarkReadAsync)
            .RequireAuthorization();

        group.MapPost("/read-all", MarkAllReadAsync)
            .RequireAuthorization();

        group.MapPost("/{recipientId:guid}/archive", ArchiveAsync)
            .RequireAuthorization();

        group.MapDelete("/{recipientId:guid}", DeleteAsync)
            .RequireAuthorization();

        group.MapPost("/broadcast", BroadcastAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetInboxAsync(
        bool? archived,
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var inbox = await notificationService.GetInboxAsync(userId, archived == true, cancellationToken);
        return Results.Ok(inbox);
    }

    private static async Task<IResult> GetUnreadCountAsync(
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var count = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Results.Ok(new UnreadCountResponse(count));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid recipientId,
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await notificationService.MarkReadAsync(recipientId, userId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> MarkAllReadAsync(
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var count = await notificationService.MarkAllReadAsync(userId, cancellationToken);
        return Results.Ok(new { count });
    }

    private static async Task<IResult> ArchiveAsync(
        Guid recipientId,
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await notificationService.ArchiveAsync(recipientId, userId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(
        Guid recipientId,
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var deleted = await notificationService.DeleteRecipientAsync(recipientId, userId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> BroadcastAsync(
        BroadcastNotificationRequest request,
        INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin(user))
        {
            return Results.Forbid();
        }

        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var validationError = ValidateBroadcastRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var notifications = await notificationService.CreateAdminNotificationAsync(
                request.Title,
                request.Body,
                request.Target,
                userId,
                cancellationToken);
            return Results.Ok(notifications);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to send notification",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult? ValidateBroadcastRequest(BroadcastNotificationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Length > 120)
        {
            errors["title"] = ["Title must be 120 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            errors["body"] = ["Body is required."];
        }
        else if (request.Body.Length > 1000)
        {
            errors["body"] = ["Body must be 1000 characters or fewer."];
        }

        if (request.Target.Type == NotificationTargetType.Group
            && string.IsNullOrWhiteSpace(request.Target.Value))
        {
            errors["target.value"] = ["Group name is required."];
        }

        if (request.Target.Type == NotificationTargetType.User
            && string.IsNullOrWhiteSpace(request.Target.Value))
        {
            errors["target.value"] = ["User id is required."];
        }

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    private static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.SystemAdmin);
}
