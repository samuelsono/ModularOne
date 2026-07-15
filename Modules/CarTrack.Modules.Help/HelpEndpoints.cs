using System.Security.Claims;
using CarTrack.Server.Users.Authorization; // RequirePermission — CarTrack.Api

namespace CarTrack.Modules.Help;

public static class HelpEndpoints
{
    private const string AdminWritePermission = "platform.help.write";

    public static RouteGroupBuilder MapHelpEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/articles", GetArticlesAsync)
            .RequireAuthorization();

        group.MapGet("/articles/{id:guid}", GetArticleAsync)
            .RequireAuthorization();

        group.MapGet("/categories", GetCategoriesAsync)
            .RequireAuthorization();

        group.MapGet("/admin/articles", GetAdminArticlesAsync)
            .RequirePermission(AdminWritePermission);

        group.MapGet("/admin/articles/{id:guid}", GetAdminArticleAsync)
            .RequirePermission(AdminWritePermission);

        group.MapPost("/admin/articles", CreateArticleAsync)
            .RequirePermission(AdminWritePermission);

        group.MapPut("/admin/articles/{id:guid}", UpdateArticleAsync)
            .RequirePermission(AdminWritePermission);

        group.MapPatch("/admin/articles/{id:guid}/publish", TogglePublishAsync)
            .RequirePermission(AdminWritePermission);

        group.MapDelete("/admin/articles/{id:guid}", DeleteArticleAsync)
            .RequirePermission(AdminWritePermission);

        return group;
    }

    private static async Task<IResult> GetArticlesAsync(
        string? search,
        string? category,
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var articles = await helpService.GetPublishedArticlesAsync(search, category, cancellationToken);
        return Results.Ok(articles);
    }

    private static async Task<IResult> GetArticleAsync(
        Guid id,
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var article = await helpService.GetPublishedArticleAsync(id, cancellationToken);
        return article is null ? Results.NotFound() : Results.Ok(article);
    }

    private static async Task<IResult> GetCategoriesAsync(
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var categories = await helpService.GetPublishedCategoriesAsync(cancellationToken);
        return Results.Ok(categories);
    }

    private static async Task<IResult> GetAdminArticlesAsync(
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var articles = await helpService.GetAdminArticlesAsync(cancellationToken);
        return Results.Ok(articles);
    }

    private static async Task<IResult> GetAdminArticleAsync(
        Guid id,
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var article = await helpService.GetAdminArticleAsync(id, cancellationToken);
        return article is null ? Results.NotFound() : Results.Ok(article);
    }

    private static async Task<IResult> CreateArticleAsync(
        SaveHelpArticleRequest request,
        IHelpService helpService,
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
            var article = await helpService.CreateArticleAsync(userId, request, cancellationToken);
            return Results.Created($"/api/help/admin/articles/{article.Id}", article);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create article",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateArticleAsync(
        Guid id,
        SaveHelpArticleRequest request,
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        try
        {
            var article = await helpService.UpdateArticleAsync(id, request, cancellationToken);
            return article is null ? Results.NotFound() : Results.Ok(article);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update article",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> TogglePublishAsync(
        Guid id,
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var article = await helpService.TogglePublishAsync(id, cancellationToken);
        return article is null ? Results.NotFound() : Results.Ok(article);
    }

    private static async Task<IResult> DeleteArticleAsync(
        Guid id,
        IHelpService helpService,
        CancellationToken cancellationToken)
    {
        var deleted = await helpService.DeleteArticleAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
