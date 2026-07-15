namespace CarTrack.Modules.Reporting;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetDashboardsAsync)
            .RequireAuthorization();

        group.MapGet("/default", GetDefaultDashboardAsync)
            .RequireAuthorization();

        group.MapGet("/default/render", GetDefaultDashboardRenderedAsync)
            .RequireAuthorization();

        group.MapGet("/{id:guid}/render", GetDashboardRenderedAsync)
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetDashboardByIdAsync)
            .RequireAuthorization();

        group.MapPost("/", CreateDashboardAsync)
            .RequireAuthorization();

        group.MapPut("/{id:guid}", UpdateDashboardAsync)
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", DeleteDashboardAsync)
            .RequireAuthorization();

        group.MapPost("/{id:guid}/sections", CreateSectionAsync)
            .RequireAuthorization();

        group.MapPut("/sections/{sectionId:guid}", UpdateSectionAsync)
            .RequireAuthorization();

        group.MapPut("/{id:guid}/layout", ReorderLayoutAsync)
            .RequireAuthorization();

        group.MapDelete("/sections/{sectionId:guid}", DeleteSectionAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetDashboardsAsync(
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var dashboards = await dashboardService.GetAllAsync(cancellationToken);
        return Results.Ok(dashboards);
    }

    private static async Task<IResult> GetDefaultDashboardAsync(
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetDefaultAsync(cancellationToken);
        return dashboard is null ? Results.NotFound() : Results.Ok(dashboard);
    }

    private static async Task<IResult> GetDefaultDashboardRenderedAsync(
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetDefaultRenderedAsync(cancellationToken);
        return dashboard is null ? Results.NotFound() : Results.Ok(dashboard);
    }

    private static async Task<IResult> GetDashboardRenderedAsync(
        Guid id,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetRenderedAsync(id, cancellationToken);
        return dashboard is null ? Results.NotFound() : Results.Ok(dashboard);
    }

    private static async Task<IResult> GetDashboardByIdAsync(
        Guid id,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetByIdAsync(id, cancellationToken);
        return dashboard is null ? Results.NotFound() : Results.Ok(dashboard);
    }

    private static async Task<IResult> CreateDashboardAsync(
        SaveDashboardRequest request,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(dashboardService.ValidateDashboard(request));
        if (validationError is not null)
        {
            return validationError;
        }

        var dashboard = await dashboardService.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/dashboards/{dashboard.Id}", dashboard);
    }

    private static async Task<IResult> UpdateDashboardAsync(
        Guid id,
        SaveDashboardRequest request,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(dashboardService.ValidateDashboard(request));
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var dashboard = await dashboardService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(dashboard);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Dashboard not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    private static async Task<IResult> DeleteDashboardAsync(
        Guid id,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        await dashboardService.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateSectionAsync(
        Guid id,
        SaveDashboardSectionRequest request,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(dashboardService.ValidateSection(request));
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var section = await dashboardService.CreateSectionAsync(id, request, cancellationToken);
            return Results.Created($"/api/dashboards/sections/{section.Id}", section);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Dashboard not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["parentSectionId"] = [ex.Message],
            });
        }
    }

    private static async Task<IResult> UpdateSectionAsync(
        Guid sectionId,
        SaveDashboardSectionRequest request,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(dashboardService.ValidateSection(request));
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var section = await dashboardService.UpdateSectionAsync(sectionId, request, cancellationToken);
            return Results.Ok(section);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Section not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["parentSectionId"] = [ex.Message],
            });
        }
    }

    private static async Task<IResult> ReorderLayoutAsync(
        Guid id,
        ReorderDashboardLayoutRequest request,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (request.Sections.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sections"] = ["At least one section is required."],
            });
        }

        try
        {
            var dashboard = await dashboardService.ReorderLayoutAsync(id, request, cancellationToken);
            return Results.Ok(dashboard);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Dashboard not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    private static async Task<IResult> DeleteSectionAsync(
        Guid sectionId,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        await dashboardService.DeleteSectionAsync(sectionId, cancellationToken);
        return Results.NoContent();
    }

    private static IResult? ToValidationResult(IReadOnlyDictionary<string, string[]>? errors) =>
        errors is null ? null : Results.ValidationProblem(errors);
}
