using CarTrack.Infrastructure.Messaging;
using CarTrack.Infrastructure.Messaging.Events;

namespace CarTrack.Modules.Fleet;

public sealed class FleetLifecycleNotifier(IEventBus eventBus) : IFleetLifecycleNotifier
{
    public Task NotifyVehicleCreatedAsync(
        string registration,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new VehicleCreatedEvent(registration, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyVehicleUpdatedAsync(
        string registration,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new VehicleUpdatedEvent(registration, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyVehicleDeletedAsync(
        int deletedCount,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new VehicleDeletedEvent(deletedCount, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyVehicleSyncedAsync(
        int created,
        int updated,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new VehicleSyncedEvent(created, updated, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyDriverCreatedAsync(
        string driverName,
        string driverId,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new DriverCreatedEvent(driverName, driverId, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyDriverUpdatedAsync(
        string driverName,
        string driverId,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new DriverUpdatedEvent(driverName, driverId, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyDriverAssignedAsync(
        string driverName,
        string registration,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new DriverAssignedEvent(driverName, registration, DateTimeOffset.UtcNow), cancellationToken);

    public Task NotifyDriverUnassignedAsync(
        string driverName,
        string registration,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new DriverUnassignedEvent(driverName, registration, DateTimeOffset.UtcNow), cancellationToken);
}
