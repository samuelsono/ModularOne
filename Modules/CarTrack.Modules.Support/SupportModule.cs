using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Support;

public sealed class SupportModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Support";
    public const string ProbeTable = "TicketCategories";

    public string Name => "support";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<SupportDbContext>("cartrack", MigrationsHistoryTable);
        builder.Services.AddScoped<ISupportService, SupportService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/support").MapSupportEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
        await SupportTicketSeeder.SeedAsync(db, cancellationToken);
    }
}
