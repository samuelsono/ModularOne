namespace CarTrack.Core;

/// <summary>
/// Audit trail fields stamped by <c>AuditableEntityInterceptor</c>.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }

    string? CreatedByUserId { get; set; }

    DateTimeOffset UpdatedAt { get; set; }

    string? UpdatedByUserId { get; set; }
}
