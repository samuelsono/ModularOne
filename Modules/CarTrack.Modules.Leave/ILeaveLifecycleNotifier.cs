namespace CarTrack.Modules.Leave;

public interface ILeaveLifecycleNotifier
{
    Task NotifyLeaveSubmittedAsync(
        string managerUserId,
        string requesterDisplayName,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken);

    Task NotifyLeaveApprovedAsync(
        string requesterUserId,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken);

    Task NotifyLeaveRejectedAsync(
        string requesterUserId,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken);

    Task NotifyLeaveCancelledAsync(
        string managerUserId,
        string requesterDisplayName,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken);
}
