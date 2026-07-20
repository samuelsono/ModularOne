using CarTrack.Core.Messaging;

namespace CarTrack.Infrastructure.Messaging.Events;

public sealed record LeaveRequestSubmittedEvent(
    Guid RequestId,
    string ManagerUserId,
    string RequesterDisplayName,
    string LeaveTypeName,
    string StartDate,
    string EndDate,
    DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record LeaveRequestApprovedEvent(
    Guid RequestId,
    string RequesterUserId,
    string LeaveTypeName,
    string StartDate,
    string EndDate,
    DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record LeaveRequestRejectedEvent(
    Guid RequestId,
    string RequesterUserId,
    string LeaveTypeName,
    string StartDate,
    string EndDate,
    DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record LeaveRequestCancelledEvent(
    Guid RequestId,
    string ManagerUserId,
    string RequesterDisplayName,
    string LeaveTypeName,
    string StartDate,
    string EndDate,
    DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record VehicleCreatedEvent(string Registration, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record VehicleUpdatedEvent(string Registration, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record VehicleDeletedEvent(int DeletedCount, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record VehicleSyncedEvent(int Created, int Updated, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record DriverCreatedEvent(string DriverName, string DriverId, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record DriverUpdatedEvent(string DriverName, string DriverId, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record DriverAssignedEvent(string DriverName, string Registration, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record DriverUnassignedEvent(string DriverName, string Registration, DateTimeOffset OccurredAt) : IIntegrationEvent;

public sealed record TenderMatchFoundEvent(
    Guid MatchId,
    string Title,
    string SourceName,
    string CanonicalUrl,
    string TargetUserId,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
