using CarTrack.Infrastructure.Messaging;
using CarTrack.Infrastructure.Messaging.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarTrack.Modules.Notifications;

/// <summary>
/// Subscribes to leave/fleet domain events and creates notification rows + SignalR push.
/// Producers publish events only; this module is the sole notification creator.
/// </summary>
public sealed class NotificationEventHandlers(
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationEventHandlers> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe<LeaveRequestSubmittedEvent>(OnLeaveSubmittedAsync);
        eventBus.Subscribe<LeaveRequestApprovedEvent>(OnLeaveApprovedAsync);
        eventBus.Subscribe<LeaveRequestRejectedEvent>(OnLeaveRejectedAsync);
        eventBus.Subscribe<LeaveRequestCancelledEvent>(OnLeaveCancelledAsync);

        eventBus.Subscribe<VehicleCreatedEvent>(OnVehicleCreatedAsync);
        eventBus.Subscribe<VehicleUpdatedEvent>(OnVehicleUpdatedAsync);
        eventBus.Subscribe<VehicleDeletedEvent>(OnVehicleDeletedAsync);
        eventBus.Subscribe<VehicleSyncedEvent>(OnVehicleSyncedAsync);

        eventBus.Subscribe<DriverCreatedEvent>(OnDriverCreatedAsync);
        eventBus.Subscribe<DriverUpdatedEvent>(OnDriverUpdatedAsync);
        eventBus.Subscribe<DriverAssignedEvent>(OnDriverAssignedAsync);
        eventBus.Subscribe<DriverUnassignedEvent>(OnDriverUnassignedAsync);

        eventBus.Subscribe<TenderMatchFoundEvent>(OnTenderMatchFoundAsync);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Task OnLeaveSubmittedAsync(LeaveRequestSubmittedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.LeaveSubmitted,
            "Leave request submitted",
            $"{e.RequesterDisplayName} submitted {e.LeaveTypeName} leave from {e.StartDate} to {e.EndDate}.",
            e.RequestId.ToString(),
            e.ManagerUserId,
            ct);

    private Task OnLeaveApprovedAsync(LeaveRequestApprovedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.LeaveApproved,
            "Leave request approved",
            $"Your {e.LeaveTypeName} leave from {e.StartDate} to {e.EndDate} was approved.",
            e.RequestId.ToString(),
            e.RequesterUserId,
            ct);

    private Task OnLeaveRejectedAsync(LeaveRequestRejectedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.LeaveRejected,
            "Leave request rejected",
            $"Your {e.LeaveTypeName} leave from {e.StartDate} to {e.EndDate} was rejected.",
            e.RequestId.ToString(),
            e.RequesterUserId,
            ct);

    private Task OnLeaveCancelledAsync(LeaveRequestCancelledEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.LeaveCancelled,
            "Leave request cancelled",
            $"{e.RequesterDisplayName} cancelled {e.LeaveTypeName} leave from {e.StartDate} to {e.EndDate}.",
            e.RequestId.ToString(),
            e.ManagerUserId,
            ct);

    private Task OnVehicleCreatedAsync(VehicleCreatedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.VehicleCreated,
            "Vehicle created",
            $"Vehicle {e.Registration} was added to the fleet.",
            e.Registration,
            targetUserId: null,
            ct);

    private Task OnVehicleUpdatedAsync(VehicleUpdatedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.VehicleUpdated,
            "Vehicle updated",
            $"Vehicle {e.Registration} details were updated.",
            e.Registration,
            targetUserId: null,
            ct);

    private Task OnVehicleDeletedAsync(VehicleDeletedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.VehicleDeleted,
            "Vehicle deleted",
            e.DeletedCount == 1
                ? "A vehicle was removed from the fleet."
                : $"{e.DeletedCount} vehicles were removed from the fleet.",
            relatedEntityId: null,
            targetUserId: null,
            ct);

    private Task OnVehicleSyncedAsync(VehicleSyncedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.VehicleSynced,
            "Fleet synced",
            $"CarTrack sync completed. {e.Created} created, {e.Updated} updated.",
            relatedEntityId: null,
            targetUserId: null,
            ct);

    private Task OnDriverCreatedAsync(DriverCreatedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.DriverCreated,
            "Driver created",
            $"Driver {e.DriverName} was added.",
            e.DriverId,
            targetUserId: null,
            ct);

    private Task OnDriverUpdatedAsync(DriverUpdatedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.DriverUpdated,
            "Driver updated",
            $"Driver {e.DriverName} details were updated.",
            e.DriverId,
            targetUserId: null,
            ct);

    private Task OnDriverAssignedAsync(DriverAssignedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.DriverAssigned,
            "Driver assigned",
            $"{e.DriverName} was assigned to vehicle {e.Registration}.",
            e.Registration,
            targetUserId: null,
            ct);

    private Task OnDriverUnassignedAsync(DriverUnassignedEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.DriverUnassigned,
            "Driver unassigned",
            $"{e.DriverName} was unassigned from vehicle {e.Registration}.",
            e.Registration,
            targetUserId: null,
            ct);

    private Task OnTenderMatchFoundAsync(TenderMatchFoundEvent e, CancellationToken ct) =>
        CreateAsync(
            NotificationActionType.TenderMatchFound,
            "New tender match",
            $"“{e.Title}” matched from {e.SourceName}.",
            e.MatchId.ToString(),
            e.TargetUserId,
            ct);

    private async Task CreateAsync(
        NotificationActionType actionType,
        string title,
        string body,
        string? relatedEntityId,
        string? targetUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notifications.CreateSystemNotificationAsync(
                actionType,
                title,
                body,
                relatedEntityId,
                targetUserId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Failed to create system notification for {ActionType}",
                actionType);
        }
    }
}
