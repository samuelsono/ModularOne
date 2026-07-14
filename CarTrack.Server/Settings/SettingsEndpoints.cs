using CarTrack.Server.Users.Authorization;

namespace CarTrack.Server.Settings;

public static class SettingsEndpoints
{
    private const string PlatformWritePermission = "platform.settings.write";

    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetOverviewAsync)
            .RequireAuthorization();

        group.MapGet("/platform", GetPlatformSettingsAsync)
            .RequireAuthorization();

        group.MapPut("/platform", UpdatePlatformSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapGet("/cartrack", GetCarTrackSettingsAsync)
            .RequireAuthorization();

        group.MapPut("/cartrack", UpdateCarTrackSettingsAsync)
            .RequireAuthorization();

        group.MapPost("/cartrack/test", TestCarTrackConnectionAsync)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetOverviewAsync(
        ICarTrackSettingsService carTrackSettingsService,
        IPlatformSettingsService platformSettingsService)
    {
        var carTrack = await carTrackSettingsService.GetAsync();
        var platform = await platformSettingsService.GetAsync();
        return Results.Ok(new AppSettingsOverviewDto(
            "TalisTrack",
            "1.0.0",
            carTrack,
            platform));
    }

    private static async Task<IResult> GetPlatformSettingsAsync(IPlatformSettingsService platformSettingsService)
    {
        var settings = await platformSettingsService.GetAsync();
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdatePlatformSettingsAsync(
        UpdatePlatformSettingsRequest request,
        IPlatformSettingsService platformSettingsService)
    {
        try
        {
            var settings = await platformSettingsService.UpdateAsync(request);
            return Results.Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["defaultModuleSlug"] = [ex.Message],
            });
        }
    }

    private static async Task<IResult> GetCarTrackSettingsAsync(ICarTrackSettingsService settingsService)
    {
        var settings = await settingsService.GetAsync();
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateCarTrackSettingsAsync(
        UpdateCarTrackSettingsRequest request,
        ICarTrackSettingsService settingsService)
    {
        try
        {
            var settings = await settingsService.UpdateAsync(request);
            return Results.Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [ex.Message],
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "CarTrack credentials incomplete",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> TestCarTrackConnectionAsync(ICarTrackSettingsService settingsService)
    {
        var result = await settingsService.TestConnectionAsync();
        return Results.Ok(result);
    }
}
