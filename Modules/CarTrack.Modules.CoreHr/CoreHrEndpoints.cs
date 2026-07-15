using CarTrack.Server.Users.Authorization; // RequirePermission — CarTrack.Api

namespace CarTrack.Modules.CoreHr;

public static class CoreHrEndpoints
{
    public static RouteGroupBuilder MapCoreHrEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/companies", GetCompaniesAsync)
            .RequirePermission("core.companies.read");

        group.MapPost("/companies", CreateCompanyAsync)
            .RequirePermission("core.companies.write");

        group.MapPut("/companies/{id:guid}", UpdateCompanyAsync)
            .RequirePermission("core.companies.write");

        group.MapGet("/departments", GetDepartmentsAsync)
            .RequirePermission("core.departments.read");

        group.MapPost("/departments", CreateDepartmentAsync)
            .RequirePermission("core.departments.write");

        group.MapPut("/departments/{id:guid}", UpdateDepartmentAsync)
            .RequirePermission("core.departments.write");

        group.MapGet("/positions", GetPositionsAsync)
            .RequirePermission("core.positions.read");

        group.MapPost("/positions", CreatePositionAsync)
            .RequirePermission("core.positions.write");

        group.MapPut("/positions/{id:guid}", UpdatePositionAsync)
            .RequirePermission("core.positions.write");

        return group;
    }

    private static async Task<IResult> GetCompaniesAsync(
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        var items = await coreHrService.GetCompaniesAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateCompanyAsync(
        SaveCompanyRequest request,
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await coreHrService.CreateCompanyAsync(request, cancellationToken);
            return Results.Created($"/api/core/companies/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create company",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateCompanyAsync(
        Guid id,
        SaveCompanyRequest request,
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await coreHrService.UpdateCompanyAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update company",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetDepartmentsAsync(
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        var items = await coreHrService.GetDepartmentsAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateDepartmentAsync(
        SaveDepartmentRequest request,
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await coreHrService.CreateDepartmentAsync(request, cancellationToken);
            return Results.Created($"/api/core/departments/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create department",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateDepartmentAsync(
        Guid id,
        SaveDepartmentRequest request,
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await coreHrService.UpdateDepartmentAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update department",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetPositionsAsync(
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        var items = await coreHrService.GetPositionsAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreatePositionAsync(
        SavePositionRequest request,
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await coreHrService.CreatePositionAsync(request, cancellationToken);
            return Results.Created($"/api/core/positions/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create position",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdatePositionAsync(
        Guid id,
        SavePositionRequest request,
        ICoreHrService coreHrService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await coreHrService.UpdatePositionAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update position",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
