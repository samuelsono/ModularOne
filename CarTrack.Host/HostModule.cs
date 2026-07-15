using CarTrack.Api;
using CarTrack.Infrastructure.Messaging;
using CarTrack.Infrastructure.Persistence;
using CarTrack.Modules.Reporting;
using CarTrack.Server.CarTrack;
using CarTrack.Server.Data;
using CarTrack.Server.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CarTrack.Host;

/// <summary>
/// Thin host composition: Identity DbContext migrate, CarTrack HTTP client,
/// and B6 reporting query executor (composes module DbContexts).
/// </summary>
public sealed class HostModule : IModule
{
    public string Name => "host";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AuditableEntityInterceptor>();
        builder.Services.AddSingleton<IDbContextOptionsConfiguration<ApplicationDbContext>, AuditableDbContextOptionsConfiguration<ApplicationDbContext>>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IEventBus, InProcessEventBus>();

        // B6 — cross-module report queries live on the Host intentionally.
        builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
        builder.Services.AddScoped<IApiTableReportExecutor, ApiTableReportExecutor>();

        builder.Services.AddHttpClient<ICarTrackApiClient, CarTrackApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CarTrackOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Feature endpoints map via their own IModule.
    }

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
