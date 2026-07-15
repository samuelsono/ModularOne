using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Reporting;

public sealed class ReportingModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Reporting";
    public const string ProbeTable = "Dashboards";

    public string Name => "reporting";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<ReportingDbContext>("cartrack", MigrationsHistoryTable);
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IReportService, ReportService>();
        // B6: ReportQueryService + ApiTableReportExecutor stay Host (cross-module DbContexts).
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/reports").MapReportEndpoints();
        endpoints.MapGroup("/api/dashboards").MapDashboardEndpoints();
    }

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        await DashboardSeeder.SeedAsync(db, cancellationToken);
    }
}
