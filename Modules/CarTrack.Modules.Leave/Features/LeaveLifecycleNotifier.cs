using CarTrack.Infrastructure.Messaging;
using CarTrack.Infrastructure.Messaging.Events;

namespace CarTrack.Modules.Leave;

public sealed class LeaveLifecycleNotifier(IEventBus eventBus) : ILeaveLifecycleNotifier
{
    public Task NotifyLeaveSubmittedAsync(
        string managerUserId,
        string requesterDisplayName,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new LeaveRequestSubmittedEvent(
                requestId,
                managerUserId,
                requesterDisplayName,
                leaveTypeName,
                startDate,
                endDate,
                DateTimeOffset.UtcNow),
            cancellationToken);

    public Task NotifyLeaveApprovedAsync(
        string requesterUserId,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new LeaveRequestApprovedEvent(
                requestId,
                requesterUserId,
                leaveTypeName,
                startDate,
                endDate,
                DateTimeOffset.UtcNow),
            cancellationToken);

    public Task NotifyLeaveRejectedAsync(
        string requesterUserId,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new LeaveRequestRejectedEvent(
                requestId,
                requesterUserId,
                leaveTypeName,
                startDate,
                endDate,
                DateTimeOffset.UtcNow),
            cancellationToken);

    public Task NotifyLeaveCancelledAsync(
        string managerUserId,
        string requesterDisplayName,
        string leaveTypeName,
        string startDate,
        string endDate,
        Guid requestId,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new LeaveRequestCancelledEvent(
                requestId,
                managerUserId,
                requesterDisplayName,
                leaveTypeName,
                startDate,
                endDate,
                DateTimeOffset.UtcNow),
            cancellationToken);
}
