namespace CarTrack.Core.Messaging;

/// <summary>Marker for cross-module integration events.</summary>
public interface IIntegrationEvent
{
    DateTimeOffset OccurredAt { get; }
}
