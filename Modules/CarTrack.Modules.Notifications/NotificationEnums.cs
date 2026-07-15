namespace CarTrack.Modules.Notifications;

public enum NotificationCategory
{
    SystemAction,
    AdminBroadcast,
}

public enum NotificationActionType
{
    VehicleCreated,
    VehicleUpdated,
    VehicleDeleted,
    VehicleSynced,
    DriverCreated,
    DriverUpdated,
    DriverDeleted,
    DriverAssigned,
    DriverUnassigned,
    LeaveSubmitted,
    LeaveApproved,
    LeaveRejected,
    LeaveCancelled,
    Custom,
}

public enum NotificationTargetType
{
    Everyone,
    Group,
    User,
}
