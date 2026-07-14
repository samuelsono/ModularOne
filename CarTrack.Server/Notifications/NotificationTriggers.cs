using CarTrack.Server.Data;

namespace CarTrack.Server.Notifications;

public static class NotificationTriggers
{
    public static Task NotifyVehicleCreatedAsync(
        INotificationService notificationService,
        string registration,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.VehicleCreated,
            "Vehicle created",
            $"Vehicle {registration} was added to the fleet.",
            registration,
            cancellationToken: cancellationToken);

    public static Task NotifyVehicleUpdatedAsync(
        INotificationService notificationService,
        string registration,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.VehicleUpdated,
            "Vehicle updated",
            $"Vehicle {registration} details were updated.",
            registration,
            cancellationToken: cancellationToken);

    public static Task NotifyVehicleDeletedAsync(
        INotificationService notificationService,
        int deletedCount,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.VehicleDeleted,
            "Vehicle deleted",
            deletedCount == 1
                ? "A vehicle was removed from the fleet."
                : $"{deletedCount} vehicles were removed from the fleet.",
            cancellationToken: cancellationToken);

    public static Task NotifyVehicleSyncedAsync(
        INotificationService notificationService,
        int created,
        int updated,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.VehicleSynced,
            "Fleet synced",
            $"CarTrack sync completed. {created} created, {updated} updated.",
            cancellationToken: cancellationToken);

    public static Task NotifyDriverCreatedAsync(
        INotificationService notificationService,
        string driverName,
        string driverId,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.DriverCreated,
            "Driver created",
            $"Driver {driverName} was added.",
            driverId,
            cancellationToken: cancellationToken);

    public static Task NotifyDriverUpdatedAsync(
        INotificationService notificationService,
        string driverName,
        string driverId,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.DriverUpdated,
            "Driver updated",
            $"Driver {driverName} details were updated.",
            driverId,
            cancellationToken: cancellationToken);

    public static Task NotifyDriverAssignedAsync(
        INotificationService notificationService,
        string driverName,
        string registration,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.DriverAssigned,
            "Driver assigned",
            $"{driverName} was assigned to vehicle {registration}.",
            registration,
            cancellationToken: cancellationToken);

    public static Task NotifyDriverUnassignedAsync(
        INotificationService notificationService,
        string driverName,
        string registration,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.DriverUnassigned,
            "Driver unassigned",
            $"{driverName} was unassigned from vehicle {registration}.",
            registration,
            cancellationToken: cancellationToken);

    public static Task NotifyLeaveSubmittedAsync(
        INotificationService notificationService,
        string managerUserId,
        string requesterDisplayName,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.LeaveSubmitted,
            "Leave request submitted",
            $"{requesterDisplayName} submitted {leaveTypeName} leave from {startDate} to {endDate}.",
            requestId.ToString(),
            managerUserId,
            cancellationToken);

    public static Task NotifyLeaveApprovedAsync(
        INotificationService notificationService,
        string requesterUserId,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.LeaveApproved,
            "Leave request approved",
            $"Your {leaveTypeName} leave from {startDate} to {endDate} was approved.",
            requestId.ToString(),
            requesterUserId,
            cancellationToken);

    public static Task NotifyLeaveRejectedAsync(
        INotificationService notificationService,
        string requesterUserId,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.LeaveRejected,
            "Leave request rejected",
            $"Your {leaveTypeName} leave from {startDate} to {endDate} was rejected.",
            requestId.ToString(),
            requesterUserId,
            cancellationToken);

    public static Task NotifyLeaveCancelledAsync(
        INotificationService notificationService,
        string managerUserId,
        string requesterDisplayName,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        notificationService.CreateSystemNotificationAsync(
            NotificationActionType.LeaveCancelled,
            "Leave request cancelled",
            $"{requesterDisplayName} cancelled {leaveTypeName} leave from {startDate} to {endDate}.",
            requestId.ToString(),
            managerUserId,
            cancellationToken);
}
