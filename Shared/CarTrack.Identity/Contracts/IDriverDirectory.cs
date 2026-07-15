namespace CarTrack.Identity.Contracts;

/// <summary>
/// Cross-module driver lookup for user linking / RLS without Fleet EF types.
/// Implemented by the Host (Legacy/Fleet) until Drivers move to FleetDbContext.
/// </summary>
public interface IDriverDirectory
{
    Task<bool> ExistsAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<DriverUserSource?> GetForUserCreateAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<DriverScopeInfo?> GetScopeInfoAsync(Guid driverId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, DriverLinkSummary>> GetLinkSummariesAsync(
        IEnumerable<Guid> driverIds,
        CancellationToken cancellationToken = default);
}

public sealed record DriverUserSource(
    Guid Id,
    string DriverCode,
    string FirstName,
    string LastName,
    string WorkEmail,
    string? EmployeeNumber,
    string? JobTitle,
    string? Department,
    string? Branch);

public sealed record DriverScopeInfo(
    Guid Id,
    string LicenceNumber,
    string DriverCode,
    string? CarTrackDriverId,
    string? Branch);

public sealed record DriverLinkSummary(Guid Id, string DriverCode, string DisplayName);
