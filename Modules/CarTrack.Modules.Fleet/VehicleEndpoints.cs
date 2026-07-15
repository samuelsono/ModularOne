using System.Globalization;
using CarTrack.Server.CarTrack;
using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Fleet;

public static class VehicleEndpoints
{
    public static RouteGroupBuilder MapVehicleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetVehiclesAsync)
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetVehicleByIdAsync)
            .RequireAuthorization();

        group.MapPost("/", CreateVehicleAsync)
            .RequireAuthorization();

        group.MapPut("/{id:guid}", UpdateVehicleAsync)
            .RequireAuthorization();

        group.MapPost("/delete", DeleteVehiclesAsync)
            .RequireAuthorization();

        group.MapPost("/sync", SyncVehiclesAsync)
            .RequireAuthorization();

        group.MapGet("/{registration}/events", GetVehicleEventsAsync)
            .RequireAuthorization();

        group.MapGet("/{registration}/trips", GetVehicleTripsAsync)
            .RequireAuthorization();

        group.MapGet("/{registration}/trips/events", GetTripEventsAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetVehiclesAsync(
        IVehicleService vehicleService,
        CancellationToken cancellationToken)
    {
        var vehicles = await vehicleService.GetAllAsync(cancellationToken);
        return Results.Ok(vehicles);
    }

    private static async Task<IResult> GetVehicleByIdAsync(
        Guid id,
        IVehicleService vehicleService,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicleService.GetByIdAsync(id, cancellationToken);
        return vehicle is null ? Results.NotFound() : Results.Ok(vehicle);
    }

    private static async Task<IResult> CreateVehicleAsync(
        CreateVehicleRequest request,
        IVehicleService vehicleService,
        IFleetLifecycleNotifier fleetLifecycleNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(VehicleEndpoints));
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber)
            || string.IsNullOrWhiteSpace(request.Make)
            || string.IsNullOrWhiteSpace(request.Model))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["registrationNumber"] = ["Registration number is required."],
                ["make"] = ["Make is required."],
                ["model"] = ["Model is required."],
            });
        }

        try
        {
            var vehicle = await vehicleService.CreateLocalAsync(request, cancellationToken);
            await TryNotifyAsync(
                () => fleetLifecycleNotifier.NotifyVehicleCreatedAsync(
                    vehicle.RegistrationNumber,
                    cancellationToken),
                logger);
            return Results.Created($"/api/vehicles/{vehicle.Id}", vehicle);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Vehicle already exists",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> UpdateVehicleAsync(
        Guid id,
        UpdateVehicleRequest request,
        IVehicleService vehicleService,
        IFleetLifecycleNotifier fleetLifecycleNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(VehicleEndpoints));
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber)
            || string.IsNullOrWhiteSpace(request.Make)
            || string.IsNullOrWhiteSpace(request.Model))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["registrationNumber"] = ["Registration number is required."],
                ["make"] = ["Make is required."],
                ["model"] = ["Model is required."],
            });
        }

        try
        {
            var vehicle = await vehicleService.UpdateLocalAsync(id, request, cancellationToken);
            await TryNotifyAsync(
                () => fleetLifecycleNotifier.NotifyVehicleUpdatedAsync(
                    vehicle.RegistrationNumber,
                    cancellationToken),
                logger);
            return Results.Ok(vehicle);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Vehicle already exists",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> DeleteVehiclesAsync(
        DeleteVehiclesRequest request,
        IVehicleService vehicleService,
        IFleetLifecycleNotifier fleetLifecycleNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(VehicleEndpoints));
        if (request.Ids is null || request.Ids.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ids"] = ["At least one vehicle id is required."],
            });
        }

        var deletedCount = await vehicleService.DeleteAsync(request.Ids, cancellationToken);
        if (deletedCount == 0)
        {
            return Results.NotFound(new { detail = "No matching vehicles were found." });
        }

        await TryNotifyAsync(
            () => fleetLifecycleNotifier.NotifyVehicleDeletedAsync(
                deletedCount,
                cancellationToken),
            logger);

        return Results.Ok(new DeleteVehiclesResponse(deletedCount));
    }

    private static async Task<IResult> SyncVehiclesAsync(
        IVehicleService vehicleService,
        IFleetLifecycleNotifier fleetLifecycleNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(VehicleEndpoints));
        var result = await vehicleService.SyncFromCarTrackAsync(cancellationToken);
        await TryNotifyAsync(
            () => fleetLifecycleNotifier.NotifyVehicleSyncedAsync(
                result.Created,
                result.Updated,
                cancellationToken),
            logger);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetVehicleEventsAsync(
        string registration,
        string? start,
        string? end,
        int? page,
        int? perPage,
        IVehicleService vehicleService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(registration))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["registration"] = ["Registration is required."],
            });
        }

        var (startTimestamp, endTimestamp, rangeError) = ResolveRange(start, end);
        if (rangeError is not null)
        {
            return rangeError;
        }

        try
        {
            var events = await vehicleService.GetEventsAsync(
                registration,
                startTimestamp,
                endTimestamp,
                page ?? 1,
                perPage ?? 15,
                cancellationToken);
            return Results.Ok(events);
        }
        catch (CarTrackApiException ex)
        {
            return MapCarTrackError("events", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Access denied",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> GetVehicleTripsAsync(
        string registration,
        string? start,
        string? end,
        int? page,
        int? perPage,
        IVehicleService vehicleService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(registration))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["registration"] = ["Registration is required."],
            });
        }

        var (startTimestamp, endTimestamp, rangeError) = ResolveRange(start, end);
        if (rangeError is not null)
        {
            return rangeError;
        }

        try
        {
            var trips = await vehicleService.GetTripsAsync(
                registration,
                startTimestamp,
                endTimestamp,
                page ?? 1,
                perPage ?? 15,
                cancellationToken);
            return Results.Ok(trips);
        }
        catch (CarTrackApiException ex)
        {
            return MapCarTrackError("trips", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Access denied",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> GetTripEventsAsync(
        string registration,
        string? start,
        string? end,
        IVehicleService vehicleService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(registration))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["registration"] = ["Registration is required."],
            });
        }

        var (startTimestamp, endTimestamp, rangeError) = ResolveRequiredRange(start, end);
        if (rangeError is not null)
        {
            return rangeError;
        }

        try
        {
            var events = await vehicleService.GetAllEventsInRangeAsync(
                registration,
                startTimestamp,
                endTimestamp,
                cancellationToken);
            return Results.Ok(events);
        }
        catch (CarTrackApiException ex)
        {
            return MapCarTrackError("events", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "Access denied",
                detail: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static (DateTimeOffset Start, DateTimeOffset End, IResult? Error) ResolveRequiredRange(
        string? start,
        string? end)
    {
        var startTimestamp = ParseTimestamp(start);
        var endTimestamp = ParseTimestamp(end);

        if (startTimestamp is null || endTimestamp is null)
        {
            return (default, default, Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["start"] = ["Start timestamp is required."],
                ["end"] = ["End timestamp is required."],
            }));
        }

        var resolvedStart = startTimestamp.Value;
        var resolvedEnd = endTimestamp.Value;

        if (resolvedEnd < resolvedStart)
        {
            return (resolvedStart, resolvedEnd, Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["end"] = ["End timestamp must be after start timestamp."],
            }));
        }

        return (resolvedStart, resolvedEnd, null);
    }

    private static (DateTimeOffset Start, DateTimeOffset End, IResult? Error) ResolveRange(
        string? start,
        string? end)
    {
        // Default to today (UTC day start through now) when no range is supplied.
        var now = DateTimeOffset.UtcNow;
        var startTimestamp = ParseTimestamp(start) ?? new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var endTimestamp = ParseTimestamp(end) ?? now;

        if (endTimestamp < startTimestamp)
        {
            return (startTimestamp, endTimestamp, Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["end"] = ["End timestamp must be after start timestamp."],
            }));
        }

        return (startTimestamp, endTimestamp, null);
    }

    private static IResult MapCarTrackError(string resource, CarTrackApiException ex) =>
        Results.Problem(
            title: $"CarTrack {resource} request failed",
            detail: ex.StatusCode == 422
                ? "The requested date range is invalid. Data is only available for approximately the last 5 years."
                : ex.Message,
            statusCode: ex.StatusCode == 422
                ? StatusCodes.Status422UnprocessableEntity
                : StatusCodes.Status502BadGateway);

    private static DateTimeOffset? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        return null;
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
