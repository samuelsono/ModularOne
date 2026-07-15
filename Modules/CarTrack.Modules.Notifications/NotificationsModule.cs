using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;

namespace CarTrack.Modules.Notifications;

public sealed class NotificationsModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Notifications";
    public const string ProbeTable = "Notifications";

    public string Name => "notifications";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.AddModuleNpgsqlDbContext<NotificationsDbContext>("cartrack", MigrationsHistoryTable);
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddHostedService<NotificationCleanupService>();
        builder.Services.AddHostedService<NotificationEventHandlers>();
        builder.Services.AddSignalR();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/notifications").MapNotificationEndpoints();
        endpoints.MapHub<NotificationHub>("/hubs/notifications");
    }

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
