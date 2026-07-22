using CarTrack.Server.Users.Authorization;

namespace CarTrack.Modules.Settings;

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
            .RequirePermission(PlatformWritePermission);

        group.MapPut("/cartrack", UpdateCarTrackSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapPost("/cartrack/test", TestCarTrackConnectionAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapGet("/auth/google", GetGoogleAuthSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapPut("/auth/google", UpdateGoogleAuthSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapGet("/auth/microsoft", GetMicrosoftAuthSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapPut("/auth/microsoft", UpdateMicrosoftAuthSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapGet("/email", GetEmailSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapPut("/email", UpdateEmailSettingsAsync)
            .RequirePermission(PlatformWritePermission);

        group.MapPost("/email/test", TestEmailSettingsAsync)
            .RequirePermission(PlatformWritePermission);

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

    private static async Task<IResult> GetGoogleAuthSettingsAsync(IExternalAuthSettingsService settingsService)
    {
        var settings = await settingsService.GetGoogleAsync();
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateGoogleAuthSettingsAsync(
        UpdateExternalAuthSettingsRequest request,
        IExternalAuthSettingsService settingsService)
    {
        try
        {
            var settings = await settingsService.UpdateGoogleAsync(request);
            return Results.Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientId"] = [ex.Message],
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Google auth credentials incomplete",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetMicrosoftAuthSettingsAsync(IExternalAuthSettingsService settingsService)
    {
        var settings = await settingsService.GetMicrosoftAsync();
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateMicrosoftAuthSettingsAsync(
        UpdateExternalAuthSettingsRequest request,
        IExternalAuthSettingsService settingsService)
    {
        try
        {
            var settings = await settingsService.UpdateMicrosoftAsync(request);
            return Results.Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientId"] = [ex.Message],
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Microsoft auth credentials incomplete",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetEmailSettingsAsync(IEmailSettingsService settingsService)
    {
        var settings = await settingsService.GetAsync();
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateEmailSettingsAsync(
        UpdateEmailSettingsRequest request,
        IEmailSettingsService settingsService)
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
                title: "Email settings incomplete",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> TestEmailSettingsAsync(
        TestEmailSettingsRequest request,
        IEmailSettingsService settingsService)
    {
        var result = await settingsService.TestAsync(request);
        return Results.Ok(result);
    }
}
