using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Help;

public sealed class HelpModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Help";
    public const string ProbeTable = "HelpArticles";

    public string Name => "help";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<HelpDbContext>("cartrack", MigrationsHistoryTable);
        builder.Services.AddScoped<IHelpService, HelpService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/help").MapHelpEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HelpDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
