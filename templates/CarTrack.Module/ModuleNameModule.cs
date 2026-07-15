using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.ModuleName;

public sealed class ModuleNameModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_ModuleName";
    public const string ProbeTable = "ModuleNamePlaceholder";

    public string Name => "modulekey";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<ModuleNameDbContext>("cartrack", MigrationsHistoryTable);
        // builder.Services.AddScoped<IModuleNameService, ModuleNameService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/modulekey").MapModuleNameEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ModuleNameDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
