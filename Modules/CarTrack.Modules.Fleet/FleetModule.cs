using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarTrack.Modules.Fleet;

public sealed class FleetModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Fleet";
    public const string ProbeTable = "Vehicles";

    public string Name => "fleet";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton<AuditableEntityInterceptor>();

        builder.AddModuleNpgsqlDbContext<FleetDbContext>(
            "cartrack",
            MigrationsHistoryTable,
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.AddInterceptors(interceptor);
            });

        builder.Services.AddScoped<IVehicleService, VehicleService>();
        builder.Services.AddScoped<IDriverService, DriverService>();
        builder.Services.AddScoped<IFleetLifecycleNotifier, FleetLifecycleNotifier>();
        builder.Services.AddScoped<IDriverDirectory, DriverDirectory>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/vehicles").MapVehicleEndpoints();
        endpoints.MapGroup("/api/drivers").MapDriverEndpoints();
    }

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
