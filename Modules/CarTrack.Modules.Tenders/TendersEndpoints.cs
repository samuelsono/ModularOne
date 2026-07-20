using System.Security.Claims;
using CarTrack.Server.Users.Authorization;

namespace CarTrack.Modules.Tenders;

public static class TendersEndpoints
{
    public static RouteGroupBuilder MapTendersEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/health", () => Results.Ok(new
            {
                module = "tenders",
                status = "ok",
                phase = "5-adapters",
            }))
            .RequirePermission("tenders.results.read");

        group.MapGet("/sources", ListSourcesAsync)
            .RequirePermission("tenders.sources.read");
        group.MapGet("/sources/{id:guid}", GetSourceAsync)
            .RequirePermission("tenders.sources.read");
        group.MapPost("/sources", CreateSourceAsync)
            .RequirePermission("tenders.sources.write");
        group.MapPut("/sources/{id:guid}", UpdateSourceAsync)
            .RequirePermission("tenders.sources.write");
        group.MapDelete("/sources/{id:guid}", DeleteSourceAsync)
            .RequirePermission("tenders.sources.write");

        group.MapGet("/queries", ListQueriesAsync)
            .RequirePermission("tenders.queries.read");
        group.MapGet("/queries/{id:guid}", GetQueryAsync)
            .RequirePermission("tenders.queries.read");
        group.MapPost("/queries", CreateQueryAsync)
            .RequirePermission("tenders.queries.write");
        group.MapPut("/queries/{id:guid}", UpdateQueryAsync)
            .RequirePermission("tenders.queries.write");
        group.MapDelete("/queries/{id:guid}", DeleteQueryAsync)
            .RequirePermission("tenders.queries.write");

        group.MapGet("/results", ListResultsAsync)
            .RequirePermission("tenders.results.read");
        group.MapGet("/results/export", ExportResultsAsync)
            .RequirePermission("tenders.results.read");
        group.MapPost("/results/bulk-seen", BulkMarkSeenAsync)
            .RequirePermission("tenders.results.write");
        group.MapPost("/results/bulk-archive", BulkArchiveAsync)
            .RequirePermission("tenders.results.write");
        group.MapPost("/results/{id:guid}/refresh-documents", RefreshDocumentsAsync)
            .RequirePermission("tenders.results.write");
        group.MapGet("/results/{id:guid}", GetResultAsync)
            .RequirePermission("tenders.results.read");
        group.MapPost("/results/{id:guid}/seen", MarkSeenAsync)
            .RequirePermission("tenders.results.write");
        group.MapPost("/results/{id:guid}/archive", ArchiveAsync)
            .RequirePermission("tenders.results.write");

        group.MapGet("/runs", ListRunsAsync)
            .RequirePermission("tenders.runs.read");
        group.MapGet("/runs/{id:guid}", GetRunAsync)
            .RequirePermission("tenders.runs.read");
        group.MapPost("/runs", CreateRunAsync)
            .RequirePermission("tenders.runs.write");

        group.MapGet("/subscriptions", GetSubscriptionAsync)
            .RequirePermission("tenders.results.read");
        group.MapPut("/subscriptions", UpsertSubscriptionAsync)
            .RequirePermission("tenders.results.write");
        group.MapDelete("/subscriptions", DeleteSubscriptionAsync)
            .RequirePermission("tenders.results.write");

        return group;
    }

    private static async Task<IResult> ListSourcesAsync(
        ITenderSourceService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ListAsync(cancellationToken));

    private static async Task<IResult> GetSourceAsync(
        Guid id,
        ITenderSourceService service,
        CancellationToken cancellationToken)
    {
        var source = await service.GetAsync(id, cancellationToken);
        return source is null ? Results.NotFound() : Results.Ok(source);
    }

    private static async Task<IResult> CreateSourceAsync(
        SaveTenderSourceRequest request,
        ITenderSourceService service,
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
            var source = await service.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"/api/tenders/sources/{source.Id}", source);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to create source", ex.Message);
        }
    }

    private static async Task<IResult> UpdateSourceAsync(
        Guid id,
        SaveTenderSourceRequest request,
        ITenderSourceService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var source = await service.UpdateAsync(id, request, cancellationToken);
            return source is null ? Results.NotFound() : Results.Ok(source);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to update source", ex.Message);
        }
    }

    private static async Task<IResult> DeleteSourceAsync(
        Guid id,
        ITenderSourceService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ListQueriesAsync(
        ITenderQueryService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ListAsync(cancellationToken));

    private static async Task<IResult> GetQueryAsync(
        Guid id,
        ITenderQueryService service,
        CancellationToken cancellationToken)
    {
        var query = await service.GetAsync(id, cancellationToken);
        return query is null ? Results.NotFound() : Results.Ok(query);
    }

    private static async Task<IResult> CreateQueryAsync(
        SaveTenderQueryRequest request,
        ITenderQueryService service,
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
            var query = await service.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"/api/tenders/queries/{query.Id}", query);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to create query", ex.Message);
        }
    }

    private static async Task<IResult> UpdateQueryAsync(
        Guid id,
        SaveTenderQueryRequest request,
        ITenderQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = await service.UpdateAsync(id, request, cancellationToken);
            return query is null ? Results.NotFound() : Results.Ok(query);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to update query", ex.Message);
        }
    }

    private static async Task<IResult> DeleteQueryAsync(
        Guid id,
        ITenderQueryService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ListResultsAsync(
        TenderMatchStatus? status,
        Guid? sourceId,
        string? keyword,
        string? search,
        bool? includeExpired,
        TenderResultsScope? scope,
        ITenderMatchService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ListAsync(
            status,
            sourceId,
            keyword,
            search,
            includeExpired ?? false,
            scope ?? TenderResultsScope.All,
            GetUserId(user),
            cancellationToken));

    private static async Task<IResult> ExportResultsAsync(
        TenderMatchStatus? status,
        Guid? sourceId,
        string? keyword,
        string? search,
        bool? includeExpired,
        TenderResultsScope? scope,
        ITenderMatchService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var (fileName, content) = await service.ExportCsvAsync(
            status,
            sourceId,
            keyword,
            search,
            includeExpired ?? false,
            scope ?? TenderResultsScope.All,
            GetUserId(user),
            cancellationToken);
        return Results.File(content, "text/csv", fileName);
    }

    private static async Task<IResult> BulkMarkSeenAsync(
        BulkTenderMatchRequest request,
        ITenderMatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.BulkMarkSeenAsync(request.Ids ?? [], cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> BulkArchiveAsync(
        BulkTenderMatchRequest request,
        ITenderMatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.BulkArchiveAsync(request.Ids ?? [], cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> RefreshDocumentsAsync(
        Guid id,
        ITenderMatchService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var match = await service.RefreshDocumentsAsync(id, cancellationToken);
            return match is null ? Results.NotFound() : Results.Ok(match);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to refresh documents", ex.Message);
        }
    }

    private static async Task<IResult> GetResultAsync(
        Guid id,
        ITenderMatchService service,
        CancellationToken cancellationToken)
    {
        var match = await service.GetAsync(id, cancellationToken);
        return match is null ? Results.NotFound() : Results.Ok(match);
    }

    private static async Task<IResult> MarkSeenAsync(
        Guid id,
        ITenderMatchService service,
        CancellationToken cancellationToken)
    {
        var match = await service.MarkSeenAsync(id, cancellationToken);
        return match is null ? Results.NotFound() : Results.Ok(match);
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ITenderMatchService service,
        CancellationToken cancellationToken)
    {
        var match = await service.ArchiveAsync(id, cancellationToken);
        return match is null ? Results.NotFound() : Results.Ok(match);
    }

    private static async Task<IResult> ListRunsAsync(
        int? take,
        ITenderScrapeService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ListRunsAsync(take ?? 50, cancellationToken));

    private static async Task<IResult> GetRunAsync(
        Guid id,
        ITenderScrapeService service,
        CancellationToken cancellationToken)
    {
        var run = await service.GetRunAsync(id, cancellationToken);
        return run is null ? Results.NotFound() : Results.Ok(run);
    }

    private static async Task<IResult> CreateRunAsync(
        CreateTenderScrapeRunRequest? request,
        ITenderScrapeService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await service.EnqueueManualRunAsync(
                GetUserId(user),
                request ?? new CreateTenderScrapeRunRequest(),
                cancellationToken);
            return Results.Accepted($"/api/tenders/runs/{run.Id}", run);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to start scrape run", ex.Message);
        }
    }

    private static async Task<IResult> GetSubscriptionAsync(
        ITenderWatchSubscriptionService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var sub = await service.GetMineAsync(userId, cancellationToken);
        return Results.Ok(sub);
    }

    private static async Task<IResult> UpsertSubscriptionAsync(
        SaveTenderWatchSubscriptionRequest request,
        ITenderWatchSubscriptionService service,
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
            var sub = await service.UpsertMineAsync(userId, request, cancellationToken);
            return Results.Ok(sub);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest("Unable to save subscription", ex.Message);
        }
    }

    private static async Task<IResult> DeleteSubscriptionAsync(
        ITenderWatchSubscriptionService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var deleted = await service.DeleteMineAsync(userId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static IResult BadRequest(string title, string detail) =>
        Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
