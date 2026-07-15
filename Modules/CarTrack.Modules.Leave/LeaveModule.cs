using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarTrack.Modules.Leave;

public sealed class LeaveModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Leave";
    public const string ProbeTable = "LeaveTypes";

    public string Name => "leave";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton<AuditableEntityInterceptor>();

        builder.AddModuleNpgsqlDbContext<LeaveDbContext>(
            "cartrack",
            MigrationsHistoryTable,
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.AddInterceptors(interceptor);
            });

        builder.Services.AddScoped<ILeaveApprovalService, LeaveApprovalService>();
        builder.Services.AddScoped<ILeaveConfigurationService, LeaveConfigurationService>();
        builder.Services.AddScoped<ILeaveWorkingDaysService, LeaveWorkingDaysService>();
        builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        builder.Services.AddScoped<ILeaveBalanceStore, LeaveBalanceService>();
        builder.Services.AddScoped<ILeaveCalendarService, LeaveCalendarService>();
        builder.Services.AddScoped<ILeaveReportService, LeaveReportService>();
        builder.Services.AddScoped<ILeaveDocumentStorage, LeaveDocumentStorage>();
        builder.Services.AddScoped<ILeaveAccrualService, LeaveAccrualService>();
        builder.Services.AddScoped<ILeaveLifecycleNotifier, LeaveLifecycleNotifier>();
        builder.Services.AddScoped<IPublicHolidaySyncService, PublicHolidaySyncService>();
        builder.Services.AddHttpClient<IOpenHolidaysApiClient, OpenHolidaysApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://openholidaysapi.org/");
        });
        builder.Services.AddHostedService<LeaveAccrualBackgroundService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/leave").MapLeaveEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();
        await LeaveSeeder.SeedAsync(db, cancellationToken);
        var leaveWorkingDaysService = scope.ServiceProvider.GetRequiredService<ILeaveWorkingDaysService>();
        await LeaveSeeder.BackfillWorkingDaysAsync(db, leaveWorkingDaysService, cancellationToken);
    }
}
