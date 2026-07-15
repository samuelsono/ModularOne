using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarTrack.Modules.CoreHr;

public sealed class CoreHrModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_CoreHr";
    public const string ProbeTable = "Companies";

    public string Name => "corehr";

    public void AddModule(IHostApplicationBuilder builder)
    {
        // Shared with HostModule for ApplicationDbContext; TryAdd avoids a duplicate registration.
        builder.Services.TryAddSingleton<AuditableEntityInterceptor>();

        builder.AddModuleNpgsqlDbContext<CoreHrDbContext>(
            "cartrack",
            MigrationsHistoryTable,
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.AddInterceptors(interceptor);
            });

        builder.Services.AddScoped<ICoreHrService, CoreHrService>();
        builder.Services.AddScoped<IOrgStructureLookup, OrgStructureLookup>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/core").MapCoreHrEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreHrDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreHrDbContext>();
        await CoreHrSeeder.SeedAsync(db, cancellationToken);
    }
}
