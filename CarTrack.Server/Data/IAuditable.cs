namespace CarTrack.Server.Data;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    string? CreatedByUserId { get; set; }

    DateTimeOffset UpdatedAt { get; set; }

    string? UpdatedByUserId { get; set; }
}
