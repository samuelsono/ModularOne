namespace CarTrack.Server.Data;

/// <summary>
/// Stores fetched pages of CarTrack resources (events, trips, trip-events)
/// so repeat requests can be served locally without hitting the rate-limited API.
/// Trip-events entries store the full merged event list for a trip time window.
/// </summary>
public class CarTrackPageCache
{
    public Guid Id { get; set; }

    public required string Resource { get; set; }

    public required string Registration { get; set; }

    public int Page { get; set; }

    public int PerPage { get; set; }

    public string RangeKey { get; set; } = string.Empty;

    public required string PayloadJson { get; set; }

    public int Total { get; set; }

    public int LastPage { get; set; }

    public int CurrentPage { get; set; }

    public DateTimeOffset FetchedAt { get; set; }
}
