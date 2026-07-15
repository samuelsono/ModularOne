using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Fleet;

public static class DriverEndpoints
{
    public static RouteGroupBuilder MapDriverEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetDriversAsync)
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetDriverByIdAsync)
            .RequireAuthorization();

        group.MapPost("/", CreateDriverAsync)
            .RequireAuthorization();

        group.MapPut("/{id:guid}", UpdateDriverAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetDriversAsync(
        IDriverService driverService,
        CancellationToken cancellationToken)
    {
        var drivers = await driverService.GetAllAsync(cancellationToken);
        return Results.Ok(drivers);
    }

    private static async Task<IResult> GetDriverByIdAsync(
        Guid id,
        IDriverService driverService,
        CancellationToken cancellationToken)
    {
        var driver = await driverService.GetByIdAsync(id, cancellationToken);
        return driver is null ? Results.NotFound() : Results.Ok(driver);
    }

    private static async Task<IResult> CreateDriverAsync(
        SaveDriverRequest request,
        IDriverService driverService,
        IFleetLifecycleNotifier fleetLifecycleNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(DriverEndpoints));
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var driver = await driverService.CreateAsync(request, cancellationToken);
            await TryNotifyAsync(
                () => fleetLifecycleNotifier.NotifyDriverCreatedAsync(
                    driver.Name,
                    driver.Id,
                    cancellationToken),
                logger);
            return Results.Created($"/api/drivers/{driver.Id}", driver);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Driver already exists",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> UpdateDriverAsync(
        Guid id,
        SaveDriverRequest request,
        IDriverService driverService,
        IFleetLifecycleNotifier fleetLifecycleNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(DriverEndpoints));
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var driver = await driverService.UpdateAsync(id, request, cancellationToken);
            await TryNotifyAsync(
                () => fleetLifecycleNotifier.NotifyDriverUpdatedAsync(
                    driver.Name,
                    driver.Id,
                    cancellationToken),
                logger);
            return Results.Ok(driver);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Driver not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Driver already exists",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static IResult? ValidateRequest(SaveDriverRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errors["firstName"] = ["First name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errors["lastName"] = ["Last name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.WorkEmail))
        {
            errors["workEmail"] = ["Work email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.WorkPhone))
        {
            errors["workPhone"] = ["Work phone is required."];
        }

        if (string.IsNullOrWhiteSpace(request.LicenceNumber))
        {
            errors["licenceNumber"] = ["Licence number is required."];
        }

        if (string.IsNullOrWhiteSpace(request.IdNumber))
        {
            errors["idNumber"] = ["ID number is required."];
        }

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static async Task TryNotifyAsync(Func<Task> action, ILogger logger)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch notification.");
        }
    }
}
