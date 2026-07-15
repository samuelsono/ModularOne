namespace CarTrack.Modules.Fleet;

public interface IFleetLifecycleNotifier
{
    Task NotifyVehicleCreatedAsync(
        string registration,
        CancellationToken cancellationToken);

    Task NotifyVehicleUpdatedAsync(
        string registration,
        CancellationToken cancellationToken);

    Task NotifyVehicleDeletedAsync(
        int deletedCount,
        CancellationToken cancellationToken);

    Task NotifyVehicleSyncedAsync(
        int created,
        int updated,
        CancellationToken cancellationToken);

    Task NotifyDriverCreatedAsync(
        string driverName,
        string driverId,
        CancellationToken cancellationToken);

    Task NotifyDriverUpdatedAsync(
        string driverName,
        string driverId,
        CancellationToken cancellationToken);

    Task NotifyDriverAssignedAsync(
        string driverName,
        string registration,
        CancellationToken cancellationToken);

    Task NotifyDriverUnassignedAsync(
        string driverName,
        string registration,
        CancellationToken cancellationToken);
}
