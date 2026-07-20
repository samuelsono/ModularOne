using CarTrack.Api;
using CarTrack.Infrastructure.Auth;
using CarTrack.Infrastructure.Persistence;
using CarTrack.Server.CarTrack;

namespace CarTrack.Modules.Settings;

public sealed class SettingsModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Settings";
    public const string ProbeTable = "CarTrackSettings";

    public string Name => "settings";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<SettingsDbContext>("cartrack", MigrationsHistoryTable);

        builder.Services.Configure<CarTrackOptions>(builder.Configuration.GetSection(CarTrackOptions.SectionName));
        // DataProtection is configured once in Host Program.cs (stable app name + persisted keys).
        builder.Services.AddScoped<CarTrackCredentialProvider>();
        builder.Services.AddScoped<ICarTrackCredentialProvider>(sp => sp.GetRequiredService<CarTrackCredentialProvider>());
        builder.Services.AddScoped<IExternalAuthCredentialProvider, ExternalAuthCredentialProvider>();
        builder.Services.AddScoped<IExternalAuthSettingsService, ExternalAuthSettingsService>();
        builder.Services.AddScoped<ICarTrackSettingsService, CarTrackSettingsService>();
        builder.Services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();
        builder.Services.AddHttpClient(nameof(CarTrackSettingsService));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/settings").MapSettingsEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SettingsDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
